using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Suppliers;

[RequirePermission(Permissions.Master.SupplierManage)]
public record CreateSupplierCommand(
    Guid CompanyId,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string Address,
    string? TaxNumber,
    int PaymentTermsDays = 30,
    string? BankAccountName = null,
    string? BankAccountNumber = null,
    string? BankName = null) : IRequest<Result<Guid>>;

public class CreateSupplierCommandValidator : AbstractValidator<CreateSupplierCommand>
{
    public CreateSupplierCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class CreateSupplierCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateSupplierCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Suppliers
            .AnyAsync(s => s.CompanyId == request.CompanyId && s.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Supplier.DuplicateCode", $"Pemasok dengan kode '{codeUpper}' sudah terdaftar."));
        }

        var supplier = Supplier.Create(
            companyId: request.CompanyId,
            code: codeUpper,
            name: request.Name,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            taxNumber: request.TaxNumber,
            paymentTermsDays: request.PaymentTermsDays,
            bankAccountName: request.BankAccountName,
            bankAccountNumber: request.BankAccountNumber,
            bankName: request.BankName,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(supplier.Id);
    }
}

[RequirePermission(Permissions.Master.SupplierManage)]
public record UpdateSupplierCommand(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string Address,
    string? TaxNumber,
    int PaymentTermsDays,
    string? BankAccountName,
    string? BankAccountNumber,
    string? BankName,
    bool IsActive) : IRequest<Result>;

public class UpdateSupplierCommandValidator : AbstractValidator<UpdateSupplierCommand>
{
    public UpdateSupplierCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class UpdateSupplierCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateSupplierCommand, Result>
{
    public async Task<Result> Handle(
        UpdateSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            return Result.Failure(
                Error.NotFound("Supplier.NotFound", "Data pemasok tidak ditemukan."));
        }

        supplier.Update(
            name: request.Name,
            email: request.Email,
            phone: request.Phone,
            address: request.Address,
            taxNumber: request.TaxNumber,
            paymentTermsDays: request.PaymentTermsDays,
            bankAccountName: request.BankAccountName,
            bankAccountNumber: request.BankAccountNumber,
            bankName: request.BankName,
            isActive: request.IsActive,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.SupplierManage)]
public record DeleteSupplierCommand(Guid Id) : IRequest<Result>;

public class DeleteSupplierCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteSupplierCommand, Result>
{
    public async Task<Result> Handle(
        DeleteSupplierCommand request,
        CancellationToken cancellationToken)
    {
        var supplier = await context.Suppliers
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (supplier == null)
        {
            return Result.Failure(
                Error.NotFound("Supplier.NotFound", "Data pemasok tidak ditemukan."));
        }

        supplier.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

