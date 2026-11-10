using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Domain.Entities.Master;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Departments;

[RequirePermission(Permissions.Master.DepartmentManage)]
public record CreateDepartmentCommand(
    Guid CompanyId,
    string Code,
    string Name) : IRequest<Result<Guid>>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Perusahaan wajib dipilih.");
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20).WithMessage("Kode departemen wajib diisi (maks 20 karakter).");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Nama departemen wajib diisi (maks 100 karakter).");
    }
}

public class CreateDepartmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var codeUpper = request.Code.Trim().ToUpperInvariant();
        var exists = await context.Departments
            .AnyAsync(d => d.CompanyId == request.CompanyId && d.Code == codeUpper, cancellationToken);

        if (exists)
        {
            return Result<Guid>.Failure(Error.Conflict("Department.DuplicateCode", $"Departemen dengan kode '{codeUpper}' sudah ada di perusahaan ini."));
        }

        var department = Department.Create(
            request.CompanyId,
            codeUpper,
            request.Name,
            currentUserService.UserId ?? Guid.Empty);

        context.Departments.Add(department);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id);
    }
}

