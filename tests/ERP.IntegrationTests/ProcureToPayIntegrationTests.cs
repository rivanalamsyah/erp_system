using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Procurement;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests;

public class ProcureToPayIntegrationTests
{
    [Fact]
    public void ProcureToPay_EndToEndFlow_ShouldMaintainDataIntegrity()
    {
        // 1. Setup Master Context
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var supplier = Supplier.Create(
            companyId,
            "SPL-001",
            "PT Mitra Logistik Jaya",
            "budi@mitra.com",
            "0812345678",
            "Jakarta",
            "01.234.567.8-001.000",
            30,
            "PT Mitra Logistik Jaya",
            "1234567890",
            "BCA",
            userId
        );
        var product = Product.Create(companyId, Guid.NewGuid(), Guid.NewGuid(), "SKU-RAW-001", null, "Bahan Baku Baut Baja", "Spec M8", ProductType.Inventory, 5000m, 7500m, 100m, userId);

        // 2. Step 1: Purchase Request (PR)
        var pr = PurchaseRequest.Create(
            companyId,
            branchId,
            "PR/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            "Permintaan rutin pabrik",
            userId
        );
        pr.AddItem(product.Id, 200m, 5000m);
        pr.Status.Should().Be(PurchaseRequestStatus.Draft);

        pr.SubmitForApproval(userId);
        pr.Status.Should().Be(PurchaseRequestStatus.PendingApproval);

        pr.Approve(userId);
        pr.Status.Should().Be(PurchaseRequestStatus.Approved);

        // 3. Step 2: Purchase Order (PO)
        var po = PurchaseOrder.Create(
            companyId,
            branchId,
            supplier.Id,
            "PO/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            userId
        );
        po.AddItem(product.Id, 200m, 5000m, 0m);

        po.SubtotalAmount.Should().Be(1000000m); // 200 * 5000
        po.TaxAmount.Should().Be(110000m);       // 11% PPN
        po.TotalAmount.Should().Be(1110000m);     // 1,110,000

        po.Approve(userId);
        po.Status.Should().Be(PurchaseOrderStatus.Approved);

        // 4. Step 3: Goods Receipt (GRN) & Perpetual Inventory Impact
        var stockBalance = StockBalance.Create(companyId, warehouseId, product.Id);
        stockBalance.PhysicalQuantity.Should().Be(0m);

        var grn = GoodsReceipt.Create(
            companyId,
            branchId,
            warehouseId,
            po.Id,
            "GRN/202611/0001",
            "SJ-VENDOR-9988",
            DateOnly.FromDateTime(DateTime.Today),
            userId
        );
        grn.AddItem(po.Items.First().Id, product.Id, 200m);

        // Confirm Receipt into Warehouse
        grn.MarkReceived(userId);
        grn.Status.Should().Be(GoodsReceiptStatus.Received);

        // Perpetual Inventory update
        stockBalance.AddStock(200m, 5000m);
        stockBalance.PhysicalQuantity.Should().Be(200m);
        stockBalance.AvailableQuantity.Should().Be(200m);
        stockBalance.AverageCost.Should().Be(5000m);

        // Record PO Receipt
        po.RecordReceipt(po.Items.First().Id, 200m);
        po.Status.Should().Be(PurchaseOrderStatus.FullyReceived);

        // 5. Step 4: Vendor Bill (AP) & 3-Way Match Validation
        var bill = VendorBill.Create(
            companyId,
            branchId,
            supplier.Id,
            po.Id,
            "BILL/202611/0001",
            "INV-VEN-2026-001",
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
            1110000m,
            userId
        );
        bill.Status.Should().Be(VendorBillStatus.Draft);

        // Posting Bill to Accounts Payable
        bill.Post(userId);
        bill.Status.Should().Be(VendorBillStatus.Posted);
        bill.RemainingAmount.Should().Be(1110000m);

        // Partial payment reconciliation
        bill.RecordPayment(500000m);
        bill.Status.Should().Be(VendorBillStatus.PartiallyPaid);
        bill.RemainingAmount.Should().Be(610000m);

        // Full payment reconciliation
        bill.RecordPayment(610000m);
        bill.Status.Should().Be(VendorBillStatus.Paid);
        bill.RemainingAmount.Should().Be(0m);
    }
}
