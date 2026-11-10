using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Finance;
using ERP.Shared;
using ERP.Shared.Security;

namespace ERP.Application.Features.Finance.ChartOfAccounts;

[RequirePermission(Permissions.Finance.CoaManage)]
public record CreateChartOfAccountCommand(
    Guid CompanyId,
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    NormalBalance NormalBalance,
    Guid? ParentAccountId = null,
    bool IsHeader = false) : IRequest<Result<Guid>>;

public class CreateChartOfAccountCommandValidator : AbstractValidator<CreateChartOfAccountCommand>
{
    public CreateChartOfAccountCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.AccountCode).NotEmpty().MaximumLength(30);
        RuleFor(x => x.AccountName).NotEmpty().MaximumLength(100);
    }
}

public class CreateChartOfAccountCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateChartOfAccountCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateChartOfAccountCommand request,
        CancellationToken cancellationToken)
    {
        var codeExists = await context.ChartOfAccounts
            .AnyAsync(c => c.CompanyId == request.CompanyId && c.AccountCode == request.AccountCode.Trim().ToUpperInvariant(), cancellationToken);

        if (codeExists)
            return Result<Guid>.Failure(Error.Conflict("COA.CodeExists", $"Nomor akun {request.AccountCode} sudah terdaftar."));

        var coa = ChartOfAccount.Create(
            companyId: request.CompanyId,
            accountCode: request.AccountCode,
            accountName: request.AccountName,
            accountType: request.AccountType,
            normalBalance: request.NormalBalance,
            createdByUserId: currentUserService.UserId ?? Guid.Empty,
            parentAccountId: request.ParentAccountId,
            isHeader: request.IsHeader);

        context.ChartOfAccounts.Add(coa);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(coa.Id);
    }
}

