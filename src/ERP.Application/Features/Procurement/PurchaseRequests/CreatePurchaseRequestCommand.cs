using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Procurement;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.PurchaseRequests;

public record PrItemInput(Guid ProductId, decimal Quantity, decimal EstimatedUnitPrice);

[RequirePermission(Permissions.Procurement.PrCreate)]
public record CreatePurchaseRequestCommand(
    Guid CompanyId,
    Guid BranchId,
    DateOnly RequestDate,
    DateOnly RequiredDate,
    string? Notes,
    List<PrItemInput> Items) : IRequest<Result<Guid>>;

public class CreatePurchaseRequestCommandValidator : AbstractValidator<CreatePurchaseRequestCommand>
{
    public CreatePurchaseRequestCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Permintaan pembelian harus memuat minimal satu item produk.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Kuantitas PR harus lebih dari 0.");
        });
    }
}

public class CreatePurchaseRequestCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreatePurchaseRequestCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreatePurchaseRequestCommand request,
        CancellationToken cancellationToken)
    {
        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "PR",
            cancellationToken);

        var pr = PurchaseRequest.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            requestNumber: docNumber,
            requestDate: request.RequestDate,
            requiredDate: request.RequiredDate,
            notes: request.Notes,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            pr.AddItem(item.ProductId, item.Quantity, item.EstimatedUnitPrice);
        }

        context.PurchaseRequests.Add(pr);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(pr.Id);
    }
}

[RequirePermission(Permissions.Procurement.PrApprove)]
public record ApprovePurchaseRequestCommand(Guid Id) : IRequest<Result>;

public class ApprovePurchaseRequestCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ApprovePurchaseRequestCommand, Result>
{
    public async Task<Result> Handle(
        ApprovePurchaseRequestCommand request,
        CancellationToken cancellationToken)
    {
        var pr = await context.PurchaseRequests
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == request.Id, cancellationToken);

        if (pr == null)
            return Result.Failure(Error.NotFound("PR.NotFound", "Data Purchase Request tidak ditemukan."));

        if (pr.Status == PurchaseRequestStatus.Draft)
        {
            pr.SubmitForApproval(currentUserService.UserId ?? Guid.Empty);
        }

        pr.Approve(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
