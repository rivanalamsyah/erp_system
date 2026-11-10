using ERP.Domain.Entities.Sales;
using FluentAssertions;
using Xunit;

namespace ERP.Domain.Tests;

public class SalesTests
{
    [Fact]
    public void Quotation_AddItem_Calculates_Subtotal_Tax_And_Total_Correctly()
    {
        // Arrange
        var sq = Quotation.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SQ-2026-0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            Guid.NewGuid());

        // Act: 5 units @ 200,000 with 5% discount -> Gross: 1,000,000 - 50,000 = 950,000
        sq.AddItem(Guid.NewGuid(), 5, 200_000, 5);

        // Assert
        sq.SubtotalAmount.Should().Be(950_000);
        sq.TaxAmount.Should().Be(104_500); // 11% PPN
        sq.TotalAmount.Should().Be(1_054_500);
    }

    [Fact]
    public void SalesOrder_Confirm_SetsStatusToConfirmed()
    {
        // Arrange
        var so = SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SO-2026-0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            Guid.NewGuid());

        so.AddItem(Guid.NewGuid(), 10, 150_000);

        // Act
        var approverId = Guid.NewGuid();
        so.Confirm(approverId);

        // Assert
        so.Status.Should().Be(SalesOrderStatus.Confirmed);
        so.ApprovedByUserId.Should().Be(approverId);
    }

    [Fact]
    public void SalesOrder_RecordShipment_UpdatesPartiallyShipped_And_FullyShipped()
    {
        // Arrange
        var so = SalesOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "SO-2026-0002",
            DateOnly.FromDateTime(DateTime.Today),
            null,
            Guid.NewGuid());

        so.AddItem(Guid.NewGuid(), 20, 50_000);
        var soItemId = so.Items.First().Id;
        so.Confirm(Guid.NewGuid());

        // Act 1: Partial shipment of 8
        so.RecordShipment(soItemId, 8);
        so.Status.Should().Be(SalesOrderStatus.PartiallyShipped);
        so.Items.First().ShippedQuantity.Should().Be(8);

        // Act 2: Remaining shipment of 12
        so.RecordShipment(soItemId, 12);
        so.Status.Should().Be(SalesOrderStatus.FullyShipped);
        so.Items.First().ShippedQuantity.Should().Be(20);
    }
}

