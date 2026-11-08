using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.Deliveries;

public record DeliveryItemInput(
    Guid SoItemId,
    Guid ProductId,
    decimal QuantityShipped);

[RequirePermission(Permissions.Sales.DeliveryDispatch)]
public record CreateDeliveryCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid WarehouseId,
    Guid SalesOrderId,
    DateOnly DeliveryDate,
    string? TrackingNumber,
    List<DeliveryItemInput> Items) : IRequest<Result<Guid>>;

public class CreateDeliveryCommandValidator : AbstractValidator<CreateDeliveryCommand>
{
    public CreateDeliveryCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.WarehouseId).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Surat Jalan harus memiliki minimal 1 item pengiriman.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.SoItemId).NotEmpty();
            item.RuleFor(i => i.QuantityShipped).GreaterThan(0).WithMessage("Kuantitas kirim harus lebih dari 0.");
        });
    }
}

public class CreateDeliveryCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateDeliveryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        var so = await context.SalesOrders
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == request.SalesOrderId, cancellationToken);

        if (so == null)
            return Result<Guid>.Failure(Error.NotFound("SO.NotFound", "Data Sales Order tidak ditemukan."));

        if (so.Status != SalesOrderStatus.Confirmed && so.Status != SalesOrderStatus.PartiallyShipped)
            return Result<Guid>.Failure(Error.Validation("SO.InvalidStatus", "Hanya Sales Order yang telah dikonfirmasi yang dapat diproses surat jalannya."));

        var deliveryNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "DO",
            cancellationToken);

        var delivery = Delivery.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            warehouseId: request.WarehouseId,
            salesOrderId: request.SalesOrderId,
            deliveryNumber: deliveryNumber,
            deliveryDate: request.DeliveryDate,
            trackingNumber: request.TrackingNumber,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        // BR-SALES-003: Delivery Order Consistency - Total shipped <= Ordered
        foreach (var item in request.Items)
        {
            var soItem = so.Items.FirstOrDefault(i => i.Id == item.SoItemId);
            if (soItem == null)
                return Result<Guid>.Failure(Error.NotFound("SOItem.NotFound", $"Baris SO {item.SoItemId} tidak ditemukan."));

            var remainingToShip = soItem.Quantity - soItem.ShippedQuantity;
            if (item.QuantityShipped > remainingToShip)
            {
                return Result<Guid>.Failure(Error.Validation(
                    "BR-SALES-003",
                    $"Kuantitas kirim ({item.QuantityShipped:N2}) melebihi sisa pesanan ({remainingToShip:N2}) pada baris SO."));
            }

            delivery.AddItem(item.SoItemId, item.ProductId, item.QuantityShipped);
        }

        context.Deliveries.Add(delivery);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(delivery.Id);
    }
}

[RequirePermission(Permissions.Sales.DeliveryDispatch)]
public record DispatchDeliveryCommand(Guid Id) : IRequest<Result>;

public class DispatchDeliveryCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DispatchDeliveryCommand, Result>
{
    public async Task<Result> Handle(
        DispatchDeliveryCommand request,
        CancellationToken cancellationToken)
    {
        var delivery = await context.Deliveries
            .Include(d => d.Items)
            .Include(d => d.SalesOrder)
                .ThenInclude(s => s.Items)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (delivery == null)
            return Result.Failure(Error.NotFound("DO.NotFound", "Data Surat Jalan tidak ditemukan."));

        if (delivery.Status != DeliveryStatus.Draft)
            return Result.Failure(Error.Validation("DO.InvalidStatus", "Hanya Surat Jalan Draft yang dapat didispatch."));

        var userId = currentUserService.UserId ?? Guid.Empty;

        foreach (var item in delivery.Items)
        {
            // Record shipment on Sales Order
            delivery.SalesOrder.RecordShipment(item.SoItemId, item.QuantityShipped);

            // Deduct stock balance from warehouse (BR-INV-001 anti-negative stock enforced in DeductStock)
            var stock = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == delivery.WarehouseId && b.ProductId == item.ProductId, cancellationToken);

            if (stock == null)
            {
                return Result.Failure(Error.Validation(
                    "BR-INV-001",
                    $"Tidak terdapat saldo stok untuk produk di gudang pengiriman."));
            }

            // Release reserved stock first if any was reserved
            if (stock.ReservedQuantity > 0)
            {
                stock.ReleaseReservation(Math.Min(item.QuantityShipped, stock.ReservedQuantity));
            }

            // Deduct physical inventory
            stock.DeductStock(item.QuantityShipped);

            // Record immutable stock movement ledger entry
            var movement = StockMovement.Create(
                companyId: delivery.CompanyId,
                branchId: delivery.BranchId,
                warehouseId: delivery.WarehouseId,
                productId: item.ProductId,
                movementType: MovementType.SalesDelivery,
                quantity: item.QuantityShipped,
                direction: "OUT",
                unitCost: stock.AverageCost,
                referenceNumber: delivery.DeliveryNumber,
                createdByUserId: userId);

            context.StockMovements.Add(movement);
        }

        delivery.Dispatch(userId);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
