using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ERP.Application.Common.Interfaces;
using ERP.Application.Common.Security;
using ERP.Shared.Common;
using ERP.Shared.Security;

namespace ERP.Application.Features.Master.Departments;

[RequirePermission(Permissions.Master.DepartmentManage)]
public record UpdateDepartmentCommand(
    Guid Id,
    string Name) : IRequest<Result>;

public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().WithMessage("ID departemen wajib diisi.");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100).WithMessage("Nama departemen wajib diisi (maks 100 karakter).");
    }
}

public class UpdateDepartmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<UpdateDepartmentCommand, Result>
{
    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (department == null)
        {
            return Result.Failure(Error.NotFound("Department.NotFound", "Departemen tidak ditemukan."));
        }

        department.Update(request.Name, currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

[RequirePermission(Permissions.Master.DepartmentManage)]
public record DeleteDepartmentCommand(Guid Id) : IRequest<Result>;

public class DeleteDepartmentCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUserService)
    : IRequestHandler<DeleteDepartmentCommand, Result>
{
    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (department == null)
        {
            return Result.Failure(Error.NotFound("Department.NotFound", "Departemen tidak ditemukan."));
        }

        // Check if employees or purchase requests reference this department
        var hasEmployees = await context.Employees.AnyAsync(e => e.DepartmentId == request.Id, cancellationToken);
        if (hasEmployees)
        {
            return Result.Failure(Error.Conflict("Department.InUse", "Departemen tidak dapat dihapus karena masih memiliki data karyawan aktif."));
        }

        department.SoftDelete(currentUserService.UserId ?? Guid.Empty);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

