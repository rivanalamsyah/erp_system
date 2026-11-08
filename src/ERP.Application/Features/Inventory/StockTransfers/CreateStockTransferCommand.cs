using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Inventory;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Inventory.StockTransfers;

public record TransferItemInput(Guid ProductId, decimal Quantity);

[RequirePermission(Permissions.Inventory.TransferCreate)]
public record CreateStockTransferCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    DateOnly TransferDate,
    string? Notes,
    List<TransferItemInput> Items) : IRequest<Result<Guid>>;

public class CreateStockTransferCommandValidator : AbstractValidator<CreateStockTransferCommand>
{
    public CreateStockTransferCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.SourceWarehouseId).NotEmpty();
        RuleFor(x => x.DestinationWarehouseId).NotEmpty()
            .NotEqual(x => x.SourceWarehouseId).WithMessage("Gudang asal dan gudang tujuan tidak boleh sama.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Transfer harus memuat minimal satu item produk.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Kuantitas transfer harus lebih dari 0.");
        });
    }
}

public class CreateStockTransferCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateStockTransferCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "TRF",
            cancellationToken);

        var transfer = StockTransfer.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            transferNumber: docNumber,
            sourceWarehouseId: request.SourceWarehouseId,
            destinationWarehouseId: request.DestinationWarehouseId,
            transferDate: request.TransferDate,
            notes: request.Notes,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            transfer.AddItem(item.ProductId, item.Quantity);
        }

        context.StockTransfers.Add(transfer);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(transfer.Id);
    }
}

[RequirePermission(Permissions.Inventory.TransferCreate)]
public record DispatchStockTransferCommand(Guid Id) : IRequest<Result>;

public class DispatchStockTransferCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DispatchStockTransferCommand, Result>
{
    public async Task<Result> Handle(
        DispatchStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await context.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (transfer == null)
            return Result.Failure(Error.NotFound("Transfer.NotFound", "Data transfer antar-gudang tidak ditemukan."));

        // 1. Validasi kecukupan stok di gudang asal & kurangi stok fisik
        foreach (var item in transfer.Items)
        {
            var balance = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == transfer.SourceWarehouseId && b.ProductId == item.ProductId, cancellationToken);

            if (balance == null || balance.PhysicalQuantity < item.Quantity)
            {
                return Result.Failure(Error.Validation(
                    "Stock.Insufficient",
                    $"Stok tidak mencukupi di gudang asal untuk produk ID '{item.ProductId}'."));
            }

            var unitCost = balance.AverageCost;
            balance.DeductStock(item.Quantity);

            // 2. Buat mutasi TransferOut (Perpetual Ledger)
            var movement = StockMovement.Create(
                companyId: transfer.CompanyId,
                branchId: transfer.BranchId,
                warehouseId: transfer.SourceWarehouseId,
                productId: item.ProductId,
                movementType: MovementType.TransferOut,
                quantity: item.Quantity,
                direction: "OUT",
                unitCost: unitCost,
                referenceNumber: transfer.TransferNumber,
                createdByUserId: currentUserService.UserId ?? Guid.Empty);

            context.StockMovements.Add(movement);
        }

        transfer.Dispatch(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Inventory.TransferCreate)]
public record ReceiveStockTransferCommand(Guid Id) : IRequest<Result>;

public class ReceiveStockTransferCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ReceiveStockTransferCommand, Result>
{
    public async Task<Result> Handle(
        ReceiveStockTransferCommand request,
        CancellationToken cancellationToken)
    {
        var transfer = await context.StockTransfers
            .Include(t => t.Items)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (transfer == null)
            return Result.Failure(Error.NotFound("Transfer.NotFound", "Data transfer antar-gudang tidak ditemukan."));

        foreach (var item in transfer.Items)
        {
            // Ambil average cost dari pergerakan TransferOut sebelumnya
            var outMovement = await context.StockMovements
                .FirstOrDefaultAsync(m =>
                    m.ReferenceNumber == transfer.TransferNumber &&
                    m.ProductId == item.ProductId &&
                    m.Direction == "OUT", cancellationToken);

            var unitCost = outMovement?.UnitCost ?? 0m;

            var destBalance = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == transfer.DestinationWarehouseId && b.ProductId == item.ProductId, cancellationToken);

            if (destBalance == null)
            {
                destBalance = StockBalance.Create(transfer.CompanyId, transfer.DestinationWarehouseId, item.ProductId);
                context.StockBalances.Add(destBalance);
            }

            destBalance.AddStock(item.Quantity, unitCost);

            // Catat mutasi TransferIn
            var movement = StockMovement.Create(
                companyId: transfer.CompanyId,
                branchId: transfer.BranchId,
                warehouseId: transfer.DestinationWarehouseId,
                productId: item.ProductId,
                movementType: MovementType.TransferIn,
                quantity: item.Quantity,
                direction: "IN",
                unitCost: unitCost,
                referenceNumber: transfer.TransferNumber,
                createdByUserId: currentUserService.UserId ?? Guid.Empty);

            context.StockMovements.Add(movement);
        }

        transfer.Receive(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

