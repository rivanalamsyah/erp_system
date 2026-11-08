using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Sales;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Sales.Quotations;

public record QuotationItemInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0);

[RequirePermission(Permissions.Sales.QuotationCreate)]
public record CreateQuotationCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid CustomerId,
    DateOnly QuotationDate,
    DateOnly ExpirationDate,
    List<QuotationItemInput> Items) : IRequest<Result<Guid>>;

public class CreateQuotationCommandValidator : AbstractValidator<CreateQuotationCommand>
{
    public CreateQuotationCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.ExpirationDate).GreaterThanOrEqualTo(x => x.QuotationDate)
            .WithMessage("Masa berlaku penawaran tidak boleh sebelum tanggal penawaran dibuat.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("Penawaran harga harus memuat minimal satu item produk.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Kuantitas harus lebih dari 0.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Harga tidak boleh negatif.");
            item.RuleFor(i => i.DiscountRate).InclusiveBetween(0, 100).WithMessage("Diskon harus antara 0% dan 100%.");
        });
    }
}

public class CreateQuotationCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateQuotationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateQuotationCommand request,
        CancellationToken cancellationToken)
    {
        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "SQ",
            cancellationToken);

        var quotation = Quotation.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            customerId: request.CustomerId,
            quotationNumber: docNumber,
            quotationDate: request.QuotationDate,
            expirationDate: request.ExpirationDate,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            quotation.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountRate);
        }

        context.Quotations.Add(quotation);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(quotation.Id);
    }
}

[RequirePermission(Permissions.Sales.QuotationCreate)]
public record ApproveQuotationCommand(Guid Id) : IRequest<Result>;

public class ApproveQuotationCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ApproveQuotationCommand, Result>
{
    public async Task<Result> Handle(
        ApproveQuotationCommand request,
        CancellationToken cancellationToken)
    {
        var quotation = await context.Quotations
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken);

        if (quotation == null)
            return Result.Failure(Error.NotFound("SQ.NotFound", "Data penawaran harga tidak ditemukan."));

        quotation.Approve(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
