using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.SalesInvoices;

[RequirePermission(Permissions.Sales.InvoiceIssue)]
public record CreateSalesInvoiceCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid SalesOrderId,
    string? TaxInvoiceNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate) : IRequest<Result<Guid>>;

public class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SalesOrderId).NotEmpty();
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.InvoiceDate)
            .WithMessage("Tanggal jatuh tempo tidak boleh sebelum tanggal faktur.");
    }
}

public class CreateSalesInvoiceCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateSalesInvoiceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateSalesInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var so = await context.SalesOrders
            .FirstOrDefaultAsync(s => s.Id == request.SalesOrderId, cancellationToken);

        if (so == null)
            return Result<Guid>.Failure(Error.NotFound("SO.NotFound", "Data Sales Order tidak ditemukan."));

        if (so.Status == SalesOrderStatus.Cancelled || so.Status == SalesOrderStatus.Draft)
            return Result<Guid>.Failure(Error.Validation("SO.InvalidStatus", "Hanya Sales Order yang telah dikonfirmasi / dikirim yang dapat ditagihkan."));

        var invoiceNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "INV",
            cancellationToken);

        var invoice = SalesInvoice.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            customerId: so.CustomerId,
            salesOrderId: so.Id,
            invoiceNumber: invoiceNumber,
            taxInvoiceNumber: request.TaxInvoiceNumber,
            invoiceDate: request.InvoiceDate,
            dueDate: request.DueDate,
            subtotalAmount: so.SubtotalAmount,
            taxAmount: so.TaxAmount,
            totalAmount: so.TotalAmount,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.SalesInvoices.Add(invoice);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(invoice.Id);
    }
}

[RequirePermission(Permissions.Sales.InvoiceIssue)]
public record PostSalesInvoiceCommand(Guid Id) : IRequest<Result>;

public class PostSalesInvoiceCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<PostSalesInvoiceCommand, Result>
{
    public async Task<Result> Handle(
        PostSalesInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        var invoice = await context.SalesInvoices
            .Include(i => i.SalesOrder)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice == null)
            return Result.Failure(Error.NotFound("Invoice.NotFound", "Data faktur penjualan tidak ditemukan."));

        invoice.Post(currentUserService.UserId ?? Guid.Empty);
        invoice.SalesOrder.MarkInvoiced();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
