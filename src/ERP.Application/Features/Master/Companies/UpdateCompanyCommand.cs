using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Companies;

[RequirePermission(Permissions.Master.CompanyManage)]
public record UpdateCompanyCommand(
    Guid Id,
    string Name,
    string TaxIdentificationNumber,
    string Address,
    string? Phone,
    string? Email,
    string CurrencyCode = "IDR") : IRequest<Result>;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.TaxIdentificationNumber).NotEmpty().MaximumLength(30);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3);

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email).EmailAddress();
        });
    }
}

public class UpdateCompanyCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCompanyCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var company = await context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (company == null)
        {
            return Result.Failure(
                Error.NotFound("Company.NotFound", "Data perusahaan tidak ditemukan."));
        }

        company.Update(
            name: request.Name,
            taxIdentificationNumber: request.TaxIdentificationNumber,
            address: request.Address,
            phone: request.Phone,
            email: request.Email,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.CompanyManage)]
public record DeleteCompanyCommand(Guid Id) : IRequest<Result>;

public class DeleteCompanyCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteCompanyCommand, Result>
{
    public async Task<Result> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var company = await context.Companies
            .Include(c => c.Branches)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (company == null)
        {
            return Result.Failure(
                Error.NotFound("Company.NotFound", "Data perusahaan tidak ditemukan."));
        }

        if (company.Branches.Any(b => !b.IsDeleted))
        {
            return Result.Failure(
                Error.Validation("Company.HasActiveBranches", "Tidak dapat menghapus perusahaan yang masih memiliki cabang aktif."));
        }

        company.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

