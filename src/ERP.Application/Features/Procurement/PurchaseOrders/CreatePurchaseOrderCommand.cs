using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Procurement;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.PurchaseOrders;

public record PoItemInput(
    Guid ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountRate = 0);

[RequirePermission(Permissions.Procurement.PoCreate)]
public record CreatePurchaseOrderCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid SupplierId,
    DateOnly OrderDate,
    DateOnly? ExpectedDeliveryDate,
    List<PoItemInput> Items) : IRequest<Result<Guid>>;

public class CreatePurchaseOrderCommandValidator : AbstractValidator<CreatePurchaseOrderCommand>
{
    public CreatePurchaseOrderCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.SupplierId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty().WithMessage("Purchase Order harus memiliki minimal 1 item produk.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty();
            item.RuleFor(i => i.Quantity).GreaterThan(0).WithMessage("Kuantitas PO harus lebih dari 0.");
            item.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0).WithMessage("Harga tidak boleh negatif.");
            item.RuleFor(i => i.DiscountRate).InclusiveBetween(0, 100).WithMessage("Diskon harus antara 0% dan 100%.");
        });
    }
}

public class CreatePurchaseOrderCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreatePurchaseOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreatePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var docNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "PO",
            cancellationToken);

        var po = PurchaseOrder.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            supplierId: request.SupplierId,
            poNumber: docNumber,
            orderDate: request.OrderDate,
            expectedDeliveryDate: request.ExpectedDeliveryDate,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        foreach (var item in request.Items)
        {
            po.AddItem(item.ProductId, item.Quantity, item.UnitPrice, item.DiscountRate);
        }

        context.PurchaseOrders.Add(po);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(po.Id);
    }
}

[RequirePermission(Permissions.Procurement.PoApprove)]
public record ApprovePurchaseOrderCommand(Guid Id) : IRequest<Result>;

public class ApprovePurchaseOrderCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<ApprovePurchaseOrderCommand, Result>
{
    public async Task<Result> Handle(
        ApprovePurchaseOrderCommand request,
        CancellationToken cancellationToken)
    {
        var po = await context.PurchaseOrders
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (po == null)
            return Result.Failure(Error.NotFound("PO.NotFound", "Data Purchase Order tidak ditemukan."));

        po.Approve(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

