using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.SalesOrders;

public record SoItemInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0);

[RequirePermission(Permissions.Sales.OrderCreate)]
public record CreateSalesOrderCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid CustomerId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    List<SoItemInput> Items,
    Guid? QuotationId = null) : IRequest<Result<Guid>>;

public class CreateSalesOrderCommandValidator : AbstractValidator<CreateSalesOrderCommand>
{
    public CreateSalesOrderCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Sales Order harus memuat minimal satu item produk.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Kuantitas harus lebih dari 0.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Harga tidak boleh negatif.");
            item.RuleFor(i => i.DiscountRate).InclusiveBetween(0, 100).WithMessage("Diskon harus antara 0% dan 100%.");
        });
    }
}

public class CreateSalesOrderCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateSalesOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.CustomerId, cancellationToken);

        if (customer == null)
            return Result<Guid>.Failure(Error.NotFound("Customer.NotFound", "Data pelanggan tidak ditemukan."));

        // Calculate expected SO total
        var subtotal = request.Items.Sum(i => Math.Round(i.Quantity * i.UnitPrice * (1 - (i.DiscountRate / 100m)), 2));
        var tax = Math.Round(subtotal * 0.11m, 2);
        var expectedTotal = subtotal + tax;

        // BR-SALES-001: Credit Limit Enforcement
        if (customer.CreditLimit > 0)
        {
            var outstandingDebt = await context.SalesInvoices
                .Where(i => i.CustomerId == customer.Id && i.Status != SalesInvoiceStatus.Paid && i.Status != SalesInvoiceStatus.Cancelled)
                .SumAsync(i => i.TotalAmount - i.PaidAmount, cancellationToken);

            if (outstandingDebt + expectedTotal > customer.CreditLimit)
            {
                return Result<Guid>.Failure(Error.Validation(
                    "BR-SALES-001",
                    $"Pelanggaran batas kredit: Total piutang terbuka (Rp {outstandingDebt:N0}) ditambah nilai pesanan baru (Rp {expectedTotal:N0}) melebihi plafon kredit pelanggan (Rp {customer.CreditLimit:N0})."));
            }
        }

        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "SO",
            cancellationToken);

        var so = SalesOrder.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            customerId: request.CustomerId,
            orderNumber: docNumber,
            orderDate: request.OrderDate,
            expectedDeliveryDate: request.ExpectedDeliveryDate,
            createdByUserId: currentUserService.UserId ?? Guid.Empty,
            quotationId: request.QuotationId);

        foreach (var item in request.Items)
        {
            so.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountRate);
        }

        // If from quotation, update quotation status
        if (request.QuotationId.HasValue)
        {
            var quotation = await context.Quotations
                .FirstOrDefaultAsync(q => q.Id == request.QuotationId.Value, cancellationToken);
            quotation?.MarkConverted(currentUserService.UserId ?? Guid.Empty);
        }

        context.SalesOrders.Add(so);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(so.Id);
    }
}

[RequirePermission(Permissions.Sales.OrderApprove)]
public record ConfirmSalesOrderCommand(Guid Id, Guid WarehouseId) : IRequest<Result>;

public class ConfirmSalesOrderCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ConfirmSalesOrderCommand, Result>
{
    public async Task<Result> Handle(
        ConfirmSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        var so = await context.SalesOrders
            .Include(s => s.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (so == null)
            return Result.Failure(Error.NotFound("SO.NotFound", "Data Sales Order tidak ditemukan."));

        if (so.Status != SalesOrderStatus.Draft)
            return Result.Failure(Error.Validation("SO.InvalidStatus", "Hanya Sales Order Draft yang dapat dikonfirmasi."));

        // BR-SALES-002: No Overselling - Check Available Stock in Warehouse
        foreach (var item in so.Items)
        {
            var stock = await context.StockBalances
                .FirstOrDefaultAsync(b => b.WarehouseId == request.WarehouseId && b.ProductId == item.ProductId, cancellationToken);

            var availableStock = stock != null ? (stock.PhysicalQuantity - stock.ReservedQuantity) : 0;
            if (availableStock < item.Quantity)
            {
                return Result.Failure(Error.Validation(
                    "BR-SALES-002",
                    $"Stok bebas untuk produk '{item.Product.Name}' di gudang tidak mencukupi (Tersedia: {availableStock:N2}, Dipesan: {item.Quantity:N2}). Harap lakukan pengadaan (PR) atau transfer stok terlebih dahulu."));
            }

            // Reserve stock for this confirmed sales order
            stock?.ReserveStock(item.Quantity);
        }

        so.Confirm(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
