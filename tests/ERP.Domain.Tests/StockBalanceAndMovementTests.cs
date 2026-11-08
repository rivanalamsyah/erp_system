using FluentAssertions;
using ERP.Domain.Entities.Inventory;
using Xunit;

namespace ERP.Domain.Tests;

public class StockBalanceAndMovementTests
{
    private readonly Guid _companyId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();
    private readonly Guid _productId = Guid.NewGuid();

    [Fact]
    public void AddStock_ShouldIncreasePhysicalQuantity_AndRecalculateMovingAverageCost()
    {
        var balance = StockBalance.Create(_companyId, _warehouseId, _productId);

        // Penerimaan pertama: 10 unit @ Rp 10.000
        balance.AddStock(10m, 10000m);
        balance.PhysicalQuantity.Should().Be(10m);
        balance.AverageCost.Should().Be(10000m);

        // Penerimaan kedua: 10 unit @ Rp 20.000
        // Total valuasi = (10 * 10.000) + (10 * 20.000) = 300.000 / 20 = 15.000
        balance.AddStock(10m, 20000m);
        balance.PhysicalQuantity.Should().Be(20m);
        balance.AverageCost.Should().Be(15000m);
    }

    [Fact]
    public void DeductStock_WhenSufficientQuantity_ShouldDecreasePhysicalQuantity()
    {
        var balance = StockBalance.Create(_companyId, _warehouseId, _productId);
        balance.AddStock(50m, 1000m);

        balance.DeductStock(20m);

        balance.PhysicalQuantity.Should().Be(30m);
        balance.AverageCost.Should().Be(1000m);
    }

    [Fact]
    public void DeductStock_WhenExceedingPhysicalQuantity_ShouldThrowBR_INV_001_Exception()
    {
        var balance = StockBalance.Create(_companyId, _warehouseId, _productId);
        balance.AddStock(10m, 1000m);

        Action act = () => balance.DeductStock(15m);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BR-INV-001*stok negatif*");
    }

    [Fact]
    public void StockTransfer_Lifecycle_ShouldFollowDraftDispatchedReceivedWorkflow()
    {
        var sourceWh = Guid.NewGuid();
        var destWh = Guid.NewGuid();
        var user = Guid.NewGuid();

        var transfer = StockTransfer.Create(
            companyId: _companyId,
            branchId: Guid.NewGuid(),
            transferNumber: "TRF/HO/202611/00001",
            sourceWarehouseId: sourceWh,
            destinationWarehouseId: destWh,
            transferDate: DateOnly.FromDateTime(DateTime.UtcNow),
            notes: "Transfer rutin",
            createdByUserId: user);

        transfer.Status.Should().Be(TransferStatus.Draft);

        transfer.AddItem(_productId, 25m);
        transfer.Items.Should().HaveCount(1);

        transfer.Dispatch(user);
        transfer.Status.Should().Be(TransferStatus.Dispatched);

        transfer.Receive(user);
        transfer.Status.Should().Be(TransferStatus.Received);
    }
}

