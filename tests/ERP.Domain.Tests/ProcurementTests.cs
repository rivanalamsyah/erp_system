using ERP.Domain.Entities.Procurement;
using FluentAssertions;
using Xunit;

namespace ERP.Domain.Tests;

public class ProcurementTests
{
    [Fact]
    public void PurchaseOrder_AddItem_Calculates_Subtotal_Tax_And_Total_Correctly()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var po = PurchaseOrder.Create(
            companyId,
            branchId,
            supplierId,
            "PO-2026-0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            userId);

        // Act: 10 units @ 100,000 with 10% discount -> Gross: 1,000,000 - 100,000 = 900,000
        po.AddItem(Guid.NewGuid(), 10, 100_000, 10);

        // Assert
        po.SubtotalAmount.Should().Be(900_000);
        po.TaxAmount.Should().Be(99_000); // 11% PPN
        po.TotalAmount.Should().Be(999_000);
    }

    [Fact]
    public void PurchaseOrder_Approve_UpdatesStatusToApproved()
    {
        // Arrange
        var po = PurchaseOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "PO-2026-0002",
            DateOnly.FromDateTime(DateTime.Today),
            null,
            Guid.NewGuid());

        po.AddItem(Guid.NewGuid(), 5, 50_000);

        // Act
        var approverId = Guid.NewGuid();
        po.Approve(approverId);

        // Assert
        po.Status.Should().Be(PurchaseOrderStatus.Approved);
        po.ApprovedByUserId.Should().Be(approverId);
    }

    [Fact]
    public void PurchaseOrder_RecordReceipt_UpdatesStatusToPartiallyReceived_And_FullyReceived()
    {
        // Arrange
        var po = PurchaseOrder.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "PO-2026-0003",
            DateOnly.FromDateTime(DateTime.Today),
            null,
            Guid.NewGuid());

        po.AddItem(Guid.NewGuid(), 10, 20_000);
        var poItemId = po.Items.First().Id;
        po.Approve(Guid.NewGuid());

        // Act 1: partial receipt of 4
        po.RecordReceipt(poItemId, 4);
        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        po.Items.First().ReceivedQuantity.Should().Be(4);

        // Act 2: remaining receipt of 6
        po.RecordReceipt(poItemId, 6);
        po.Status.Should().Be(PurchaseOrderStatus.FullyReceived);
        po.Items.First().ReceivedQuantity.Should().Be(10);
    }
}
