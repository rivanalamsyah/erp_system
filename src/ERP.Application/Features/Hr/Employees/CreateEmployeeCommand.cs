using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Hr;
using ERP.Shared;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ERP.Application.Features.Hr.Employees;

public record CreateEmployeeCommand(
    Guid CompanyId,
    Guid BranchId,
    Guid DepartmentId,
    string EmployeeNumber,
    string FullName,
    string IdentityCardNumber,
    DateOnly BirthDate,
    DateOnly HireDate,
    EmploymentStatus EmploymentStatus,
    decimal BasicSalary
) : IRequest<Result<Guid>>;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.EmployeeNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.IdentityCardNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.BasicSalary).GreaterThanOrEqualTo(0);
    }
}

public class CreateEmployeeCommandHandler : IRequestHandler<CreateEmployeeCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateEmployeeCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeCommand request, CancellationToken cancellationToken)
    {
        var codeExists = await _context.Employees
            .AnyAsync(e => e.EmployeeNumber == request.EmployeeNumber && e.CompanyId == request.CompanyId, cancellationToken);

        if (codeExists)
        {
            return Result<Guid>.Failure(Error.Conflict("Employee.DuplicateCode", $"NIK / Employee number '{request.EmployeeNumber}' already exists."));
        }

        var userId = _currentUserService.UserId ?? Guid.NewGuid();

        var employee = Employee.Create(
            request.CompanyId,
            request.BranchId,
            request.DepartmentId,
            request.EmployeeNumber,
            request.FullName,
            request.IdentityCardNumber,
            request.BirthDate,
            request.HireDate,
            request.EmploymentStatus,
            request.BasicSalary,
            userId
        );

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(employee.Id);
    }
}
