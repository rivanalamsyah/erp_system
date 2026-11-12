using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Procurement;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.GoodsReceipts;

public record GrnItemInput(
    Guid PoItemId,
    Guid ProductId,
    decimal QuantityReceived);

[RequirePermission(Permissions.Procurement.GrnCreate)]
public record CreateGoodsReceiptCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid WarehouseId,
    Guid PurchaseOrderId,
    string DeliveryNoteNumber,
    DateOnly ReceiptDate,
    List<GrnItemInput> Items) : IRequest<Result<Guid>>;

public class CreateGoodsReceiptCommandValidator : AbstractValidator<CreateGoodsReceiptCommand>
{
    public CreateGoodsReceiptCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.DeliveryNoteNumber).NotEmpty().WithMessage("Nomor Surat Jalan Vendor wajib diisi.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Goods Receipt harus memiliki minimal satu item.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.PoItemId).NotEmpty();
            item.RuleFor(i => i.QuantityReceived).GreaterThan(0).WithMessage("Kuantitas penerimaan harus lebih dari 0.");
        });
    }
}

public class CreateGoodsReceiptCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateGoodsReceiptCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateGoodsReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var po = await context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

        if (po == null)
            return Result<Guid>.Failure(Error.NotFound("PO.NotFound", "Purchase Order tidak ditemukan."));

        if (po.Status != PurchaseOrderStatus.Approved && po.Status != PurchaseOrderStatus.PartiallyReceived)
            return Result<Guid>.Failure(Error.Validation("PO.InvalidStatus", "Hanya PO dengan status Disetujui atau Diterima Sebagian yang dapat diproses penerimaan barangnya."));

        var receiptNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "GRN",
            cancellationToken);

        var grn = GoodsReceipt.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            warehouseId: request.WarehouseId,
            purchaseOrderId: request.PurchaseOrderId,
            receiptNumber: receiptNumber,
            deliveryNoteNumber: request.DeliveryNoteNumber,
            receiptDate: request.ReceiptDate,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            var poItem = po.Items.FirstOrDefault(i => i.Id == item.PoItemId);
            if (poItem == null)
                return Result<Guid>.Failure(Error.NotFound("POItem.NotFound", $"Item PO {item.PoItemId} tidak ditemukan."));

            var remainingExpected = poItem.Quantity - poItem.ReceivedQuantity;
            if (item.QuantityReceived > remainingExpected)
            {
                return Result<Guid>.Failure(Error.Validation(
                    "GRN.OverReceipt",
                    $"Kuantitas penerimaan ({item.QuantityReceived}) melebihi sisa yang belum diterima ({remainingExpected}) pada PO."));
            }

            grn.AddItem(item.PoItemId, item.ProductId, item.QuantityReceived);
        }

        context.GoodsReceipts.Add(grn);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(grn.Id);
    }
}

[RequirePermission(Permissions.Procurement.GrnReceive)]
public record ReceiveGoodsReceiptCommand(Guid Id) : IRequest<Result>;

public class ReceiveGoodsReceiptCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IAccountingPostingService accountingPostingService)
    : IRequestHandler<ReceiveGoodsReceiptCommand, Result>
{
    public async Task<Result> Handle(
        ReceiveGoodsReceiptCommand request,
        CancellationToken cancellationToken)
    {
        var grn = await context.GoodsReceipts
            .Include(g => g.Items)
            .Include(g => g.PurchaseOrder)
                .ThenInclude(p => p.Items)
            .FirstOrDefaultAsync(g => g.Id == request.Id, cancellationToken);

        if (grn == null)
            return Result.Failure(Error.NotFound("GRN.NotFound", "Data Goods Receipt tidak ditemukan."));

        if (grn.Status != GoodsReceiptStatus.Draft)
            return Result.Failure(Error.Validation("GRN.InvalidStatus", "Hanya GRN dengan status Draft yang dapat diproses penerimaannya."));

        var periodCheck = await accountingPostingService.EnsureFiscalPeriodOpenAsync(grn.CompanyId, grn.ReceiptDate, cancellationToken);
        if (periodCheck.IsFailure)
            return periodCheck;

        var userId = currentUserService.UserId ?? Guid.Empty;

        // Batch load StockBalances in a single query (eliminates N+1 inside loop)
        var productIds = grn.Items.Select(i => i.ProductId).Distinct().ToList();
        var stockBalances = await context.StockBalances
            .Where(b => b.WarehouseId == grn.WarehouseId && productIds.Contains(b.ProductId))
            .ToDictionaryAsync(b => b.ProductId, cancellationToken);

        decimal totalReceiptValue = 0m;

        // Process stock inbound and update PO received quantity
        foreach (var grnItem in grn.Items)
        {
            var poItem = grn.PurchaseOrder.Items.First(p => p.Id == grnItem.PoItemId);
            grn.PurchaseOrder.RecordReceipt(grnItem.PoItemId, grnItem.QuantityReceived);

            if (!stockBalances.TryGetValue(grnItem.ProductId, out var stockBalance))
            {
                stockBalance = StockBalance.Create(grn.CompanyId, grn.WarehouseId, grnItem.ProductId);
                context.StockBalances.Add(stockBalance);
                stockBalances[grnItem.ProductId] = stockBalance;
            }

            var effectiveUnitCost = Math.Round(poItem.UnitPrice * (1 - (poItem.DiscountRate / 100m)), 4);
            totalReceiptValue += Math.Round(grnItem.QuantityReceived * effectiveUnitCost, 2);

            // Recalculate moving average cost and physical stock
            stockBalance.AddStock(grnItem.QuantityReceived, effectiveUnitCost);

            // Create perpetual stock movement ledger entry
            var movement = StockMovement.Create(
                companyId: grn.CompanyId,
                branchId: grn.BranchId,
                warehouseId: grn.WarehouseId,
                productId: grnItem.ProductId,
                movementType: MovementType.PurchaseReceipt,
                quantity: grnItem.QuantityReceived,
                direction: "IN",
                unitCost: effectiveUnitCost,
                referenceNumber: grn.ReceiptNumber,
                createdByUserId: userId);

            context.StockMovements.Add(movement);
        }

        grn.MarkReceived(userId);

        var glResult = await accountingPostingService.PostGoodsReceiptJournalAsync(grn, totalReceiptValue, userId, cancellationToken);
        if (glResult.IsFailure)
            return Result.Failure(glResult.Error);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
