using ERP.Domain.Entities.Master;

namespace ERP.Application.Features.Master.Products;

public record ProductDto(
    Guid Id,
    Guid CompanyId,
    Guid CategoryId,
    string CategoryName,
    Guid UomId,
    string UomCode,
    string Sku,
    string? Barcode,
    string Name,
    string? Description,
    ProductType ProductType,
    decimal PurchasePrice,
    decimal SellingPrice,
    decimal MinimumStockLevel,
    bool IsActive,
    DateTime CreatedAtUtc);

