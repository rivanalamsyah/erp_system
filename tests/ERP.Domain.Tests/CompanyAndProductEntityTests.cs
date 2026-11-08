using FluentAssertions;
using ERP.Domain.Entities.Master;
using Xunit;

namespace ERP.Domain.Tests;

public class CompanyAndProductEntityTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _categoryId = Guid.NewGuid();
    private readonly Guid _uomId = Guid.NewGuid();

    [Fact]
    public void Company_Create_WithValidData_ShouldSucceed()
    {
        var company = Company.Create(
            code: "CMP-999",
            name: "PT Test Inovasi",
            taxIdentificationNumber: "12.345.678.9-012.000",
            address: "Jakarta",
            phone: "+62 21 123456",
            email: "test@inovasi.id",
            createdByUserId: _userId);

        company.Id.Should().NotBeEmpty();
        company.Code.Should().Be("CMP-999");
        company.Name.Should().Be("PT Test Inovasi");
        company.CurrencyCode.Should().Be("IDR");
        company.IsDeleted.Should().BeFalse();
        company.CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Company_Create_WithEmptyCode_ShouldThrowArgumentException()
    {
        Action act = () => Company.Create(
            code: "",
            name: "PT Test",
            taxIdentificationNumber: "123",
            address: "Alamat",
            phone: null,
            email: null,
            createdByUserId: _userId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Company_SoftDelete_ShouldSetIsDeletedAndAuditFields()
    {
        var company = Company.Create(
            code: "CMP-DEL",
            name: "PT Hapus",
            taxIdentificationNumber: "123",
            address: "Alamat",
            phone: null,
            email: null,
            createdByUserId: _userId);

        company.SoftDelete(_userId);

        company.IsDeleted.Should().BeTrue();
        company.DeletedByUserId.Should().Be(_userId);
        company.DeletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Product_Create_WithNegativePrice_ShouldThrowArgumentOutOfRangeException()
    {
        Action act = () => Product.Create(
            companyId: _companyId,
            categoryId: _categoryId,
            uomId: _uomId,
            sku: "SKU-001",
            barcode: null,
            name: "Barang Test",
            description: null,
            productType: ProductType.Inventory,
            purchasePrice: -100m,
            sellingPrice: 500m,
            minimumStockLevel: 0m,
            createdByUserId: _userId);

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Harga beli tidak boleh negatif*");
    }

    [Fact]
    public void Product_Create_WithValidData_ShouldSucceed()
    {
        var product = Product.Create(
            companyId: _companyId,
            categoryId: _categoryId,
            uomId: _uomId,
            sku: "sku-valid-01",
            barcode: "89912345",
            name: "Kertas A4 70gr",
            description: "Rim 500 lembar",
            productType: ProductType.Inventory,
            purchasePrice: 40000m,
            sellingPrice: 50000m,
            minimumStockLevel: 10m,
            createdByUserId: _userId);

        product.Sku.Should().Be("SKU-VALID-01");
        product.PurchasePrice.Should().Be(40000m);
        product.SellingPrice.Should().Be(50000m);
        product.IsActive.Should().BeTrue();
    }
}

