namespace ERP.Application.Features.Sales.SalesInvoices;

public record SalesInvoiceDto(
    Guid Id,
    string InvoiceNumber,
    string? TaxInvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    Guid SalesOrderId,
    string OrderNumber,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    decimal SubtotalAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RemainingAmount,
    string Status,
    DateTime CreatedAtUtc);
