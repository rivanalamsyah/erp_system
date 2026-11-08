using Microsoft.EntityFrameworkCore;
using ERP.Domain.Entities.Audit;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Procurement;

namespace ERP.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    // Master Data
    DbSet<Company> Companies { get; }
    DbSet<Branch> Branches { get; }
    DbSet<Department> Departments { get; }
    DbSet<UnitOfMeasure> UnitsOfMeasure { get; }
    DbSet<ProductCategory> ProductCategories { get; }
    DbSet<Product> Products { get; }
    DbSet<Customer> Customers { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<DocumentSequence> DocumentSequences { get; }

    // Inventory
    DbSet<Warehouse> Warehouses { get; }
    DbSet<StockBalance> StockBalances { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockTransfer> StockTransfers { get; }
    DbSet<StockTransferItem> StockTransferItems { get; }
    DbSet<StockAdjustment> StockAdjustments { get; }
    DbSet<StockAdjustmentItem> StockAdjustmentItems { get; }

    // Procurement
    DbSet<PurchaseRequest> PurchaseRequests { get; }
    DbSet<PurchaseRequestItem> PurchaseRequestItems { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptItem> GoodsReceiptItems { get; }
    DbSet<VendorBill> VendorBills { get; }

    // Sales
    DbSet<ERP.Domain.Entities.Sales.Quotation> Quotations { get; }
    DbSet<ERP.Domain.Entities.Sales.QuotationItem> QuotationItems { get; }
    DbSet<ERP.Domain.Entities.Sales.SalesOrder> SalesOrders { get; }
    DbSet<ERP.Domain.Entities.Sales.SalesOrderItem> SalesOrderItems { get; }
    DbSet<ERP.Domain.Entities.Sales.Delivery> Deliveries { get; }
    DbSet<ERP.Domain.Entities.Sales.DeliveryItem> DeliveryItems { get; }
    DbSet<ERP.Domain.Entities.Sales.SalesInvoice> SalesInvoices { get; }

    // Finance
    DbSet<ERP.Domain.Entities.Finance.ChartOfAccount> ChartOfAccounts { get; }
    DbSet<ERP.Domain.Entities.Finance.FiscalPeriod> FiscalPeriods { get; }
    DbSet<ERP.Domain.Entities.Finance.JournalEntry> JournalEntries { get; }
    DbSet<ERP.Domain.Entities.Finance.JournalLine> JournalLines { get; }
    DbSet<ERP.Domain.Entities.Finance.CashBankAccount> CashBankAccounts { get; }
    DbSet<ERP.Domain.Entities.Finance.Payment> Payments { get; }
    DbSet<ERP.Domain.Entities.Finance.PaymentAllocation> PaymentAllocations { get; }

    // HR
    DbSet<ERP.Domain.Entities.Hr.Employee> Employees { get; }
    DbSet<ERP.Domain.Entities.Hr.AttendanceRecord> AttendanceRecords { get; }
    DbSet<ERP.Domain.Entities.Hr.LeaveType> LeaveTypes { get; }
    DbSet<ERP.Domain.Entities.Hr.LeaveRequest> LeaveRequests { get; }
    DbSet<ERP.Domain.Entities.Hr.PayrollPeriod> PayrollPeriods { get; }
    DbSet<ERP.Domain.Entities.Hr.PayrollItem> PayrollItems { get; }

    // Audit
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

