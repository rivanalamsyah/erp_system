namespace ERP.Application.Features.Inventory.Warehouses;

public record WarehouseDto(
    Guid Id,
    Guid CompanyId,
    Guid BranchId,
    string BranchName,
    string Code,
    string Name,
    string? Location,
    bool IsActive,
    DateTime CreatedAtUtc);

