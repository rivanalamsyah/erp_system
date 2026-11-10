using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Sales;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests;

public class OrderToCashIntegrationTests
{
    [Fact]
    public void OrderToCash_EndToEndFlow_ShouldMaintainReservationAndStockIntegrity()
    {
        // 1. Setup Master Context
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var customer = Customer.Create(
            companyId,
            "CUST-001",
            "PT Sukses Berdikari Mandiri",
            "handoko@sukses.com",
            "0811998877",
            "Surabaya",
            "Surabaya",
            "02.345.678.9-002.000",
            50000000m,
            30,
            userId
        );
        var product = Product.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), "SKU-FIN-001", null, "Panel Distribusi 3-Phase", "Spec 400V", ProductType.Inventory, 1000000m, 1500000m, 5m, userId);

        // Pre-condition: Warehouse has 10 units in stock
        var stockBalance = StockBalance.Create(companyId, warehouseId, product.Id);
        stockBalance.AddStock(10m, 1000000m);
        stockBalance.PhysicalQuantity.Should().Be(10m);
        stockBalance.AvailableQuantity.Should().Be(10m);

        // 2. Step 1: Sales Quotation (SQ)
        var sq = Quotation.Create(
            companyId,
            branchId,
            customer.Id,
            "SQ/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(14)),
            userId
        );
        sq.AddItem(product.Id, 4m, 1500000m, 0m);
        sq.SubtotalAmount.Should().Be(6000000m);
        sq.TaxAmount.Should().Be(660000m);
        sq.TotalAmount.Should().Be(6660000m);

        sq.Approve(userId);
        sq.Status.Should().Be(QuotationStatus.Approved);

        // 3. Step 2: Sales Order (SO) with Credit Limit & Stock Reservation
        var so = SalesOrder.Create(
            companyId,
            branchId,
            customer.Id,
            "SO/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            userId,
            sq.Id
        );
        so.AddItem(product.Id, 4m, 1500000m, 0m);

        // BR-SALES-001: Credit Limit Check
        (so.TotalAmount <= customer.CreditLimit).Should().BeTrue();

        // BR-SALES-002: Available Stock Reservation
        (stockBalance.AvailableQuantity >= 4m).Should().BeTrue();
        stockBalance.ReserveStock(4m);
        stockBalance.ReservedQuantity.Should().Be(4m);
        stockBalance.AvailableQuantity.Should().Be(6m); // 10 - 4

        so.Confirm(userId);
        so.Status.Should().Be(SalesOrderStatus.Confirmed);

        // 4. Step 3: Delivery Order (DO) / Surat Jalan & Warehouse Stock Dispatch
        var delivery = Delivery.Create(
            companyId,
            branchId,
            warehouseId,
            so.Id,
            "DO/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            "B 1234 CD",
            userId
        );
        delivery.AddItem(so.Items.First().Id, product.Id, 4m);

        // Dispatch goods from warehouse
        delivery.Dispatch(userId);
        delivery.Status.Should().Be(DeliveryStatus.Dispatched);

        // Release reservation & deduct physical inventory
        stockBalance.ReleaseReservation(4m);
        stockBalance.DeductStock(4m);
        stockBalance.PhysicalQuantity.Should().Be(6m);
        stockBalance.ReservedQuantity.Should().Be(0m);
        stockBalance.AvailableQuantity.Should().Be(6m);

        // Mark SO Shipped
        so.RecordShipment(so.Items.First().Id, 4m);
        so.Status.Should().Be(SalesOrderStatus.FullyShipped);

        // 5. Step 4: Sales Invoice (SI) & Payment Allocation
        var invoice = SalesInvoice.Create(
            companyId,
            branchId,
            customer.Id,
            so.Id,
            "INV/202611/0001",
            "010.000-26.00000001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            6000000m,
            660000m,
            6660000m,
            userId
        );
        invoice.Post(userId);
        invoice.Status.Should().Be(SalesInvoiceStatus.Posted);
        invoice.RemainingAmount.Should().Be(6660000m);

        // Record customer payment
        invoice.RecordPayment(6660000m);
        invoice.Status.Should().Be(SalesInvoiceStatus.Paid);
        invoice.RemainingAmount.Should().Be(0m);

        so.MarkInvoiced();
        so.Status.Should().Be(SalesOrderStatus.Invoiced);
    }
}
