using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Inventory;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Inventory.StockAdjustments;

public record AdjustmentItemInput(Guid ProductId, decimal PhysicalQuantity);

[RequirePermission(Permissions.Inventory.AdjustmentApprove)]
public record CreateStockAdjustmentCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid WarehouseId,
    DateOnly AdjustmentDate,
    string Reason,
    List<AdjustmentItemInput> Items) : IRequest<Result<Guid>>;

public class CreateStockAdjustmentCommandValidator : AbstractValidator<CreateStockAdjustmentCommand>
{
    public CreateStockAdjustmentCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Items).NotEmpty().WithMessage("Penyesuaian stok harus memuat minimal satu item opname.");
    }
}

public class CreateStockAdjustmentCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateStockAdjustmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateStockAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "ADJ",
            cancellationToken);

        var adjustment = StockAdjustment.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            warehouseId: request.WarehouseId,
            adjustmentNumber: docNumber,
            adjustmentDate: request.AdjustmentDate,
            reason: request.Reason,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            var balance = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == request.WarehouseId && b.ProductId == item.ProductId, cancellationToken);

            var systemQty = balance?.PhysicalQuantity ?? 0m;
            var unitCost = balance?.AverageCost ?? 0m;

            adjustment.AddItem(item.ProductId, systemQty, item.PhysicalQuantity, unitCost);
        }

        context.StockAdjustments.Add(adjustment);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(adjustment.Id);
    }
}

[RequirePermission(Permissions.Inventory.AdjustmentApprove)]
public record ApproveStockAdjustmentCommand(Guid Id) : IRequest<Result>;

public class ApproveStockAdjustmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ApproveStockAdjustmentCommand, Result>
{
    public async Task<Result> Handle(
        ApproveStockAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        var adjustment = await context.StockAdjustments
            .Include(a => a.Items)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (adjustment == null)
            return Result.Failure(Error.NotFound("Adjustment.NotFound", "Data penyesuaian stok tidak ditemukan."));

        foreach (var item in adjustment.Items)
        {
            var balance = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == adjustment.WarehouseId && b.ProductId == item.ProductId, cancellationToken);

            if (balance == null)
            {
                balance = StockBalance.Create(adjustment.CompanyId, adjustment.WarehouseId, item.ProductId);
                context.StockBalances.Add(balance);
            }

            var diff = item.DifferenceQuantity;
            if (diff > 0)
            {
                // Penyesuaian positif (stok fisik lebih banyak)
                balance.AddStock(diff, item.UnitCost);

                var movement = StockMovement.Create(
                    companyId: adjustment.CompanyId,
                    branchId: adjustment.BranchId,
                    warehouseId: adjustment.WarehouseId,
                    productId: item.ProductId,
                    movementType: MovementType.AdjustmentPositive,
                    quantity: diff,
                    direction: "IN",
                    unitCost: item.UnitCost,
                    referenceNumber: adjustment.AdjustmentNumber,
                    createdByUserId: currentUserService.UserId ?? Guid.Empty);

                context.StockMovements.Add(movement);
            }
            else if (diff < 0)
            {
                // Penyesuaian negatif (stok fisik hilang / rusak)
                var deductQty = Math.Abs(diff);
                balance.DeductStock(deductQty);

                var movement = StockMovement.Create(
                    companyId: adjustment.CompanyId,
                    branchId: adjustment.BranchId,
                    warehouseId: adjustment.WarehouseId,
                    productId: item.ProductId,
                    movementType: MovementType.AdjustmentNegative,
                    quantity: deductQty,
                    direction: "OUT",
                    unitCost: item.UnitCost,
                    referenceNumber: adjustment.AdjustmentNumber,
                    createdByUserId: currentUserService.UserId ?? Guid.Empty);

                context.StockMovements.Add(movement);
            }
        }

        adjustment.Approve(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

