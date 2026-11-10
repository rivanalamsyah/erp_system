namespace ERP.Application.Features.Reports;

public record TopCustomerDto(
    Guid CustomerId,
    string CustomerName,
    int InvoiceCount,
    decimal TotalRevenue
);

public record TopProductDto(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    decimal TotalQuantitySold,
    decimal TotalRevenue
);

public record SalesAnalyticsDto(
    decimal TotalInvoiced,
    decimal TotalCollected,
    decimal TotalOutstandingAr,
    int TotalOrders,
    int TotalInvoices,
    List<TopCustomerDto> TopCustomers,
    List<TopProductDto> TopProducts
);

public record ProcurementAnalyticsDto(
    decimal TotalPurchaseOrders,
    decimal TotalBilled,
    decimal TotalPaid,
    decimal TotalOutstandingAp,
    int TotalPoCount,
    int TotalBillCount
);

