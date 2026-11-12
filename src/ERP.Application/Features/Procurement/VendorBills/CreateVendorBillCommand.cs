using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Procurement;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Procurement.VendorBills;

[RequirePermission(Permissions.Procurement.BillCreate)]
public record CreateVendorBillCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid PurchaseOrderId,
    string VendorInvoiceNumber,
    DateOnly BillDate,
    DateOnly DueDate) : IRequest<Result<Guid>>;

public class CreateVendorBillCommandValidator : AbstractValidator<CreateVendorBillCommand>
{
    public CreateVendorBillCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.BranchId).NotEmpty();
        RuleFor(x => x.PurchaseOrderId).NotEmpty();
        RuleFor(x => x.VendorInvoiceNumber).NotEmpty().WithMessage("Nomor Faktur Vendor wajib diisi.");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(x => x.BillDate).WithMessage("Jatuh tempo tidak boleh sebelum tanggal tagihan.");
    }
}

public class CreateVendorBillCommandHandler(
    IApplicationDbContext context,
    IDocumentSequenceService sequenceService,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateVendorBillCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateVendorBillCommand request,
        CancellationToken cancellationToken)
    {
        var po = await context.PurchaseOrders
            .FirstOrDefaultAsync(p => p.Id == request.PurchaseOrderId, cancellationToken);

        if (po == null)
            return Result<Guid>.Failure(Error.NotFound("PO.NotFound", "Data Purchase Order tidak ditemukan."));

        if (po.Status == PurchaseOrderStatus.Cancelled || po.Status == PurchaseOrderStatus.Draft)
            return Result<Guid>.Failure(Error.Validation("PO.InvalidStatus", "Hanya PO yang telah disetujui / diterima yang dapat ditagihkan."));

        var billNumber = await sequenceService.NextDocumentNumberAsync(
            request.CompanyId,
            request.BranchId,
            "BILL",
            cancellationToken);

        var bill = VendorBill.Create(
            companyId: request.CompanyId,
            branchId: request.BranchId,
            supplierId: po.SupplierId,
            purchaseOrderId: po.Id,
            billNumber: billNumber,
            vendorInvoiceNumber: request.VendorInvoiceNumber,
            billDate: request.BillDate,
            dueDate: request.DueDate,
            totalAmount: po.TotalAmount,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.VendorBills.Add(bill);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(bill.Id);
    }
}

[RequirePermission(Permissions.Procurement.BillPost)]
public record PostVendorBillCommand(Guid Id) : IRequest<Result>;

public class PostVendorBillCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService,
    IAccountingPostingService accountingPostingService)
    : IRequestHandler<PostVendorBillCommand, Result>
{
    public async Task<Result> Handle(
        PostVendorBillCommand request,
        CancellationToken cancellationToken)
    {
        var bill = await context.VendorBills
            .Include(b => b.PurchaseOrder)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (bill == null)
            return Result.Failure(Error.NotFound("Bill.NotFound", "Data tagihan vendor tidak ditemukan."));

        var periodCheck = await accountingPostingService.EnsureFiscalPeriodOpenAsync(bill.CompanyId, bill.BillDate, cancellationToken);
        if (periodCheck.IsFailure)
            return periodCheck;

        var userId = currentUserService.UserId ?? Guid.Empty;
        bill.Post(userId);
        bill.PurchaseOrder.MarkAsBilled();

        var glResult = await accountingPostingService.PostVendorBillJournalAsync(bill, userId, cancellationToken);
        if (glResult.IsFailure)
            return Result.Failure(glResult.Error);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
