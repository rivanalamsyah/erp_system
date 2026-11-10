namespace ERP.Application.Features.Master.Departments;

public record DepartmentDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Code,
    string Name,
    DateTime CreatedAtUtc);

