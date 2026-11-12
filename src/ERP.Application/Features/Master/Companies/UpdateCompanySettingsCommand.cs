using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared;
using ERP.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Master.Companies;

public record CompanySettingsDto(
    Guid CompanyId,
    string Code,
    string Name,
    string CurrencyCode,
    decimal DefaultVatRate,
    int FiscalStartMonth,
    int DefaultPaymentTermsDays,
    decimal DefaultCreditLimit,
    bool EnforceStrictCreditLimit,
    bool AlertLowStock,
    int StockWarningThresholdPercentage,
    bool AlertOverdueInvoices,
    int OverdueReminderDays);

[RequirePermission(Permissions.Master.CompanyManage)]
public record GetCompanySettingsQuery(Guid CompanyId) : IRequest<Result<CompanySettingsDto>>;

public class GetCompanySettingsQueryHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<GetCompanySettingsQuery, Result<CompanySettingsDto>>
{
    public async Task<Result<CompanySettingsDto>> Handle(
        GetCompanySettingsQuery request,
        CancellationToken cancellationToken)
    {
        var targetCompanyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var company = await context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId && !c.IsDeleted, cancellationToken);

        if (company == null)
        {
            return Result<CompanySettingsDto>.Failure(Error.NotFound("Company.NotFound", "Data perusahaan tidak ditemukan."));
        }

        return Result<CompanySettingsDto>.Success(new CompanySettingsDto(
            company.Id,
            company.Code,
            company.Name,
            company.CurrencyCode,
            company.DefaultVatRate,
            company.FiscalStartMonth,
            company.DefaultPaymentTermsDays,
            company.DefaultCreditLimit,
            company.EnforceStrictCreditLimit,
            company.AlertLowStock,
            company.StockWarningThresholdPercentage,
            company.AlertOverdueInvoices,
            company.OverdueReminderDays));
    }
}

[RequirePermission(Permissions.Master.CompanyManage)]
public record UpdateCompanySettingsCommand(
    Guid CompanyId,
    decimal DefaultVatRate,
    int FiscalStartMonth,
    int DefaultPaymentTermsDays,
    decimal DefaultCreditLimit,
    bool EnforceStrictCreditLimit,
    bool AlertLowStock,
    int StockWarningThresholdPercentage,
    bool AlertOverdueInvoices,
    int OverdueReminderDays) : IRequest<Result>;

public class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.DefaultVatRate).InclusiveBetween(0m, 100m).WithMessage("Tarif PPN harus berada di antara 0% dan 100%.");
        RuleFor(x => x.FiscalStartMonth).InclusiveBetween(1, 12).WithMessage("Bulan awal tahun fiskal harus antara 1 dan 12.");
        RuleFor(x => x.DefaultPaymentTermsDays).GreaterThanOrEqualTo(0).WithMessage("Termin pembayaran tidak boleh negatif.");
        RuleFor(x => x.DefaultCreditLimit).GreaterThanOrEqualTo(0m).WithMessage("Pagu kredit tidak boleh negatif.");
    }
}

public class UpdateCompanySettingsCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateCompanySettingsCommand, Result>
{
    public async Task<Result> Handle(
        UpdateCompanySettingsCommand request,
        CancellationToken cancellationToken)
    {
        var targetCompanyId = request.CompanyId != Guid.Empty ? request.CompanyId : currentUserService.CompanyId;

        var company = await context.Companies
            .FirstOrDefaultAsync(c => c.Id == targetCompanyId && !c.IsDeleted, cancellationToken);

        if (company == null)
        {
            return Result.Failure(Error.NotFound("Company.NotFound", "Data perusahaan tidak ditemukan."));
        }

        company.UpdateBusinessSettings(
            defaultVatRate: request.DefaultVatRate,
            fiscalStartMonth: request.FiscalStartMonth,
            defaultPaymentTermsDays: request.DefaultPaymentTermsDays,
            defaultCreditLimit: request.DefaultCreditLimit,
            enforceStrictCreditLimit: request.EnforceStrictCreditLimit,
            alertLowStock: request.AlertLowStock,
            stockWarningThresholdPercentage: request.StockWarningThresholdPercentage,
            alertOverdueInvoices: request.AlertOverdueInvoices,
            overdueReminderDays: request.OverdueReminderDays,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
