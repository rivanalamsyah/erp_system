using ERP.Domain.Common;

namespace ERP.Domain.Entities.Master;

public enum ProductType
{
    Inventory,
    Service,
    RawMaterial
}

public class Product : BaseEntity<Guid>
{
    public Guid CategoryId { get; private set; }
    public Guid UomId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public ProductType ProductType { get; private set; } = ProductType.Inventory;
    public decimal PurchasePrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public decimal MinimumStockLevel { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;
    public ProductCategory Category { get; private set; } = default!;
    public UnitOfMeasure Uom { get; private set; } = default!;

    private Product() { }

    public static Product Create(
        Guid companyId,
        Guid categoryId,
        Guid uomId,
        string sku,
        string? barcode,
        string name,
        string? description,
        ProductType productType,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal minimumStockLevel,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (purchasePrice < 0) throw new ArgumentOutOfRangeException(nameof(purchasePrice), "Harga beli tidak boleh negatif.");
        if (sellingPrice < 0) throw new ArgumentOutOfRangeException(nameof(sellingPrice), "Harga jual tidak boleh negatif.");
        if (minimumStockLevel < 0) throw new ArgumentOutOfRangeException(nameof(minimumStockLevel), "Minimum stok tidak boleh negatif.");

        return new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CategoryId = categoryId,
            UomId = uomId,
            Sku = sku.Trim().ToUpperInvariant(),
            Barcode = barcode?.Trim(),
            Name = name.Trim(),
            Description = description?.Trim(),
            ProductType = productType,
            PurchasePrice = purchasePrice,
            SellingPrice = sellingPrice,
            MinimumStockLevel = minimumStockLevel,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        Guid categoryId,
        Guid uomId,
        string? barcode,
        string name,
        string? description,
        ProductType productType,
        decimal purchasePrice,
        decimal sellingPrice,
        decimal minimumStockLevel,
        bool isActive,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (purchasePrice < 0) throw new ArgumentOutOfRangeException(nameof(purchasePrice), "Harga beli tidak boleh negatif.");
        if (sellingPrice < 0) throw new ArgumentOutOfRangeException(nameof(sellingPrice), "Harga jual tidak boleh negatif.");
        if (minimumStockLevel < 0) throw new ArgumentOutOfRangeException(nameof(minimumStockLevel), "Minimum stok tidak boleh negatif.");

        CategoryId = categoryId;
        UomId = uomId;
        Barcode = barcode?.Trim();
        Name = name.Trim();
        Description = description?.Trim();
        ProductType = productType;
        PurchasePrice = purchasePrice;
        SellingPrice = sellingPrice;
        MinimumStockLevel = minimumStockLevel;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

