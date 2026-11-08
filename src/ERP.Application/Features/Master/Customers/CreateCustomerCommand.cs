using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Customers;

[RequirePermission(Permissions.Master.CustomerManage)]
public record CreateCustomerCommand(
    Guid CompanyId,
    string Code,
    string Name,
    string? Email,
    string? Phone,
    string BillingAddress,
    string? ShippingAddress,
    string? TaxNumber,
    decimal CreditLimit = 0,
    int PaymentTermsDays = 30) : IRequest<Result<Guid>>;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Code).NotEmpty().MaximumLength(30).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BillingAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class CreateCustomerCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateCustomerCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Customers
            .AnyAsync(c => c.CompanyId == request.CompanyId && c.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Customer.DuplicateCode", $"Pelanggan dengan kode '{codeUpper}' sudah terdaftar."));
        }

        var customer = Customer.Create(
            companyId: request.CompanyId,
            code: codeUpper,
            name: request.Name,
            email: request.Email,
            phone: request.Phone,
            billingAddress: request.BillingAddress,
            shippingAddress: request.ShippingAddress,
            taxNumber: request.TaxNumber,
            creditLimit: request.CreditLimit,
            paymentTermsDays: request.PaymentTermsDays,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.Customers.Add(customer);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(customer.Id);
    }
}

[RequirePermission(Permissions.Master.CustomerManage)]
public record UpdateCustomerCommand(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string BillingAddress,
    string? ShippingAddress,
    string? TaxNumber,
    decimal CreditLimit,
    int PaymentTermsDays,
    bool IsActive) : IRequest<Result>;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.BillingAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CreditLimit).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PaymentTermsDays).GreaterThanOrEqualTo(0);
    }
}

public class UpdateCustomerCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCustomerCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null)
        {
            return Result.Failure(
                Error.NotFound("Customer.NotFound", "Data pelanggan tidak ditemukan."));
        }

        customer.Update(
            name: request.Name,
            email: request.Email,
            phone: request.Phone,
            billingAddress: request.BillingAddress,
            shippingAddress: request.ShippingAddress,
            taxNumber: request.TaxNumber,
            creditLimit: request.CreditLimit,
            paymentTermsDays: request.PaymentTermsDays,
            isActive: request.IsActive,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.CustomerManage)]
public record DeleteCustomerCommand(Guid Id) : IRequest<Result>;

public class DeleteCustomerCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteCustomerCommand, Result>
{
    public async Task<Result> Handle(
        DeleteCustomerCommand request,
        CancellationToken cancellationToken)
    {
        var customer = await context.Customers
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (customer == null)
        {
            return Result.Failure(
                Error.NotFound("Customer.NotFound", "Data pelanggan tidak ditemukan."));
        }

        customer.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

