using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Branches;

[RequirePermission(Permissions.Master.CompanyManage)]
public record CreateBranchCommand(
    Guid CompanyId,
    string Code,
    string Name,
    string? Address,
    string? Phone,
    bool IsHeadOffice = false) : IRequest<Result<Guid>>;

public class CreateBranchCommandValidator : AbstractValidator<CreateBranchCommand>
{
    public CreateBranchCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Perusahaan induk wajib dipilih.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(300);
    }
}

public class CreateBranchCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateBranchCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(
        CreateBranchCommand request,
        CancellationToken cancellationToken)
    {
        var company = await context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            return Result<Guid>.Failure(
                Error.NotFound("Company.NotFound", "Perusahaan induk yang dipilih tidak valid."));
        }

        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Branches
            .AnyAsync(b => b.CompanyId == request.CompanyId && b.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Branch.DuplicateCode", $"Cabang dengan kode '{codeUpper}' sudah ada di perusahaan ini."));
        }

        var branch = Branch.Create(
            companyId: request.CompanyId,
            code: codeUpper,
            name: request.Name,
            address: request.Address,
            phone: request.Phone,
            isHeadOffice: request.IsHeadOffice,
            createdByUserId: currentUserService.UserId ?? Guid.Empty);

        context.Branches.Add(branch);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(branch.Id);
    }
}

[RequirePermission(Permissions.Master.CompanyManage)]
public record UpdateBranchCommand(
    Guid Id,
    string Name,
    string? Address,
    string? Phone,
    bool IsHeadOffice) : IRequest<Result>;

public class UpdateBranchCommandValidator : AbstractValidator<UpdateBranchCommand>
{
    public UpdateBranchCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Address).MaximumLength(300);
    }
}

public class UpdateBranchCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateBranchCommand, Result>
{
    public async Task<Result> Handle(
        UpdateBranchCommand request,
        CancellationToken cancellationToken)
    {
        var branch = await context.Branches
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (branch == null)
        {
            return Result.Failure(
                Error.NotFound("Branch.NotFound", "Data cabang tidak ditemukan."));
        }

        branch.Update(
            name: request.Name,
            address: request.Address,
            phone: request.Phone,
            isHeadOffice: request.IsHeadOffice,
            updatedByUserId: currentUserService.UserId ?? Guid.Empty);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.CompanyManage)]
public record DeleteBranchCommand(Guid Id) : IRequest<Result>;

public class DeleteBranchCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteBranchCommand, Result>
{
    public async Task<Result> Handle(
        DeleteBranchCommand request,
        CancellationToken cancellationToken)
    {
        var branch = await context.Branches
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (branch == null)
        {
            return Result.Failure(
                Error.NotFound("Branch.NotFound", "Data cabang tidak ditemukan."));
        }

        if (branch.IsHeadOffice)
        {
            return Result.Failure(
                Error.Validation("Branch.CannotDeleteHeadOffice", "Kantor Pusat (Head Office) tidak dapat dihapus."));
        }

        branch.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

