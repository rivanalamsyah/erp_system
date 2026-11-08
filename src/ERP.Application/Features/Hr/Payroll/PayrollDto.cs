using ERP.Domain.Entities.Hr;

namespace ERP.Application.Features.Hr.Payroll;

public record PayrollItemDto(
    Guid Id,
    Guid EmployeeId,
    string EmployeeNumber,
    string EmployeeFullName,
    string DepartmentName,
    decimal BasicSalary,
    decimal Allowances,
    decimal Deductions,
    decimal IncomeTaxPph21,
    decimal NetSalary,
    bool IsPaid
);

public record PayrollPeriodDto(
    Guid Id,
    Guid CompanyId,
    string PeriodName,
    DateOnly StartDate,
    DateOnly EndDate,
    PayrollStatus Status,
    decimal TotalBasicSalary,
    decimal TotalAllowances,
    decimal TotalDeductions,
    decimal TotalTax,
    decimal TotalNetSalary,
    int EmployeeCount,
    List<PayrollItemDto> Items
);
