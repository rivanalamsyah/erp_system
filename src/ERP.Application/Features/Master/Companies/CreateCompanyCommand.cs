using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Companies;

[RequirePermission(Permissions.Master.CompanyManage)]
public record CreateCompanyCommand(
    string Code,
    string Name,
    string TaxIdentificationNumber,
    string Address,
    string? Phone,
    string? Email,
    string CurrencyCode = "IDR") : IRequest<Result<Guid>>;

public class CreateCompanyCommandValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyCommandValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Kode perusahaan wajib diisi.")
            .MaximumLength(20).WithMessage("Kode perusahaan maksimal 20 karakter.")
            .Matches("^[A-Za-z0-9_-]+$").WithMessage("Kode perusahaan hanya boleh berupa huruf alfanumerik, dash, atau underscore.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Nama perusahaan wajib diisi.")
            .MaximumLength(150).WithMessage("Nama perusahaan maksimal 150 karakter.");

        RuleFor(x => x.TaxIdentificationNumber)
            .NotEmpty().WithMessage("Nomor Pokok Wajib Pajak (NPWP) wajib diisi.")
            .MaximumLength(30).WithMessage("NPWP maksimal 30 karakter.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Alamat kantor pusat wajib diisi.")
            .MaximumLength(300).WithMessage("Alamat maksimal 300 karakter.");

        RuleFor(x => x.CurrencyCode)
            .NotEmpty().WithMessage("Mata uang dasar wajib diisi.")
            .Length(3).WithMessage("Kode mata uang harus 3 karakter ISO (misal: IDR, USD).");

        When(x => !string.IsNullOrEmpty(x.Email), () =>
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Format email perusahaan tidak valid.");
        });
    }
}

public class CreateCompanyCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateCompanyCommand request,
        CancellationToken cancellationToken)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Companies
            .AnyAsync(c => c.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Company.DuplicateCode", $"Perusahaan dengan kode '{codeUpper}' sudah terdaftar."));
        }

        var company = Company.Create(
            code: codeUpper,
            name: request.Name,
            taxIdentificationNumber: request.TaxIdentificationNumber,
            address: request.Address,
            phone: request.Phone,
            email: request.Email,
            createdByUserId: currentUserService.UserId ?? Guid.Empty,
            currencyCode: request.CurrencyCode);

        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(company.Id);
    }
}

