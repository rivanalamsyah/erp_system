using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Finance;
using ERP.Domain.Entities.Sales;
using ERP.Domain.Entities.Procurement;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.Payments;

public record PaymentAllocationInput(Guid InvoiceId, decimal AllocatedAmount);

[RequirePermission(Permissions.Finance.PaymentProcess)]
public record CreatePaymentCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid CashBankAccountId,
    DateOnly PaymentDate,
    PaymentType PaymentType,
    Guid PartnerId,
    PaymentMethod PaymentMethod,
    decimal TotalAmount,
    List<PaymentAllocationInput> Allocations) : IRequest<Result<Guid>>;

public class CreatePaymentCommandValidator : AbstractValidator<CreatePaymentCommand>
{
    public CreatePaymentCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CashBankAccountId).NotEmpty();
        RuleFor(x => x.PartnerId).NotEmpty();
        RuleFor(x => x.TotalAmount).GreaterThan(0).WithMessage("Nominal pembayaran harus lebih besar dari 0.");
        RuleForEach(x => x.Allocations).ChildRules(alloc =>
        {
            alloc.RuleFor(a => a.InvoiceId).NotEmpty();
            alloc.RuleFor(a => a.AllocatedAmount).GreaterThan(0).WithMessage("Alokasi pembayaran harus lebih besar dari 0.");
        });
    }
}

public class CreatePaymentCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreatePaymentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var prefix = request.PaymentType == PaymentType.CustomerReceipt ? "CR" : "PV";

        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            prefix,
            cancellationToken);

        var payment = Payment.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            cashBankAccountId: request.CashBankAccountId,
            paymentNumber: docNumber,
            paymentDate: request.PaymentDate,
            paymentType: request.PaymentType,
            partnerId: request.PartnerId,
            paymentMethod: request.PaymentMethod,
            totalAmount: request.TotalAmount,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var alloc in request.Allocations)
        {
            payment.AddAllocation(alloc.InvoiceId, alloc.AllocatedAmount);
        }

        context.Payments.Add(payment);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(payment.Id);
    }
}

[RequirePermission(Permissions.Finance.PaymentProcess)]
public record PostPaymentCommand(Guid Id) : IRequest<Result>;

public class PostPaymentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<PostPaymentCommand, Result>
{
    public async Task<Result> Handle(
        PostPaymentCommand request,
        CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (payment == null)
            return Result.Failure(Error.NotFound("Payment.NotFound", "Data pembayaran tidak ditemukan."));

        if (payment.Status != PaymentStatus.Draft)
            return Result.Failure(Error.Validation("Payment.InvalidStatus", "Hanya pembayaran Draft yang dapat diposting."));

        // BR-ACC-003: Batas Alokasi Pembayaran
        if (payment.PaymentType == PaymentType.CustomerReceipt)
        {
            foreach (var alloc in payment.Allocations)
            {
                var invoice = await context.SalesInvoices
                    .FirstOrDefaultAsync(i => i.Id == alloc.InvoiceId, cancellationToken);

                if (invoice == null)
                    return Result.Failure(Error.NotFound("Invoice.NotFound", $"Faktur {alloc.InvoiceId} tidak ditemukan."));

                if (alloc.AllocatedAmount > invoice.RemainingAmount)
                {
                    return Result.Failure(Error.Validation(
                        "BR-ACC-003",
                        $"Alokasi pembayaran (Rp {alloc.AllocatedAmount:N0}) melebihi sisa piutang faktur {invoice.InvoiceNumber} (Rp {invoice.RemainingAmount:N0})."));
                }

                invoice.RecordPayment(alloc.AllocatedAmount);
            }
        }
        else // VendorPayment
        {
            foreach (var alloc in payment.Allocations)
            {
                var bill = await context.VendorBills
                    .FirstOrDefaultAsync(b => b.Id == alloc.InvoiceId, cancellationToken);

                if (bill == null)
                    return Result.Failure(Error.NotFound("Bill.NotFound", $"Tagihan vendor {alloc.InvoiceId} tidak ditemukan."));

                if (alloc.AllocatedAmount > bill.RemainingAmount)
                {
                    return Result.Failure(Error.Validation(
                        "BR-ACC-003",
                        $"Alokasi pembayaran (Rp {alloc.AllocatedAmount:N0}) melebihi sisa hutang tagihan {bill.BillNumber} (Rp {bill.RemainingAmount:N0})."));
                }

                bill.RecordPayment(alloc.AllocatedAmount);
            }
        }

        payment.Post(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

