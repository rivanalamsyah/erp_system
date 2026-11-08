namespace ERP.Application.Features.Procurement.VendorBills;

public record VendorBillDto(
    Guid Id,
    string BillNumber,
    string VendorInvoiceNumber,
    Guid SupplierId,
    string SupplierName,
    Guid PurchaseOrderId,
    string PoNumber,
    DateOnly BillDate,
    DateOnly DueDate,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string Status,
    DateTime CreatedAtUtc);
