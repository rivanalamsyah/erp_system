using FluentAssertions;
using ERP.Application.Features.Master.Products;
using ERP.Domain.Entities.Master;
using Xunit;

namespace ERP.Application.Tests;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validator_WithValidData_ShouldPass()
    {
        var command = new CreateProductCommand(
            CompanyId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            UomId: Guid.NewGuid(),
            Sku: "PRD-STL-001",
            Barcode: "899999",
            Name: "Baja Ringan 4x4",
            Description: "Baja ringan galvanis",
            ProductType: ProductType.RawMaterial,
            PurchasePrice: 85000m,
            SellingPrice: 95000m,
            MinimumStockLevel: 20m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WithNegativePrices_ShouldFailValidation()
    {
        var command = new CreateProductCommand(
            CompanyId: Guid.NewGuid(),
            CategoryId: Guid.NewGuid(),
            UomId: Guid.NewGuid(),
            Sku: "PRD-001",
            Barcode: null,
            Name: "Produk",
            Description: null,
            ProductType: ProductType.Inventory,
            PurchasePrice: -500m,
            SellingPrice: -100m,
            MinimumStockLevel: -5m);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.PurchasePrice));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.SellingPrice));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateProductCommand.MinimumStockLevel));
    }
}

