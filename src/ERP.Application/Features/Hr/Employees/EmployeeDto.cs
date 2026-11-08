using ERP.Domain.Entities.Hr;

namespace ERP.Application.Features.Hr.Employees;

public record EmployeeDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string BranchName,
    Guid DepartmentId,
    string DepartmentName,
    string EmployeeNumber,
    string FullName,
    string IdentityCardNumber,
    DateOnly BirthDate,
    DateOnly HireDate,
    EmploymentStatus EmploymentStatus,
    decimal BasicSalary,
    bool IsActive
);
