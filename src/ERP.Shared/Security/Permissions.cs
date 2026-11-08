namespace ERP.Shared.Security;

public static class Permissions
{
    public static class Security
    {
        public const string UsersManage = "security.users.manage";
        public const string RolesManage = "security.roles.manage";
        public const string AuditView = "security.audit.view";
    }

    public static class Dashboard
    {
        public const string ExecutiveView = "dashboard.executive.view";
    }

    public static class Master
    {
        public const string CompanyManage = "master.company.manage";
        public const string ProductManage = "master.product.manage";
        public const string CustomerManage = "master.customer.manage";
        public const string SupplierManage = "master.supplier.manage";
    }

    public static class Sales
    {
        public const string QuotationCreate = "sales.quotation.create";
        public const string OrderCreate = "sales.order.create";
        public const string OrderApprove = "sales.order.approve";
        public const string DeliveryDispatch = "sales.delivery.dispatch";
        public const string InvoiceIssue = "sales.invoice.issue";
    }

    public static class Procurement
    {
        public const string PrCreate = "procurement.pr.create";
        public const string PrApprove = "procurement.pr.approve";
        public const string PoCreate = "procurement.po.create";
        public const string PoApprove = "procurement.po.approve";
        public const string GrnReceive = "procurement.grn.receive";
        public const string BillVerify = "procurement.bill.verify";
    }

    public static class Inventory
    {
        public const string StockView = "inventory.stock.view";
        public const string TransferCreate = "inventory.transfer.create";
        public const string AdjustmentApprove = "inventory.adjustment.approve";
    }

    public static class Finance
    {
        public const string CoaManage = "finance.coa.manage";
        public const string JournalCreate = "finance.journal.create";
        public const string JournalPost = "finance.journal.post";
        public const string PaymentProcess = "finance.payment.process";
        public const string PeriodClose = "finance.period.close";
    }

    public static class Hr
    {
        public const string EmployeeManage = "hr.employee.manage";
        public const string AttendanceManage = "hr.attendance.manage";
        public const string LeaveApprove = "hr.leave.approve";
        public const string PayrollProcess = "hr.payroll.process";
        public const string SelfServiceView = "hr.selfservice.view";
    }

    public static class Reports
    {
        public const string FinancialView = "reports.financial.view";
        public const string ExportData = "reports.export.data";
    }

    public record PermissionDefinition(string Code, string Module, string Description);

    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        // Security
        new(Security.UsersManage, "Security", "Kelola Akun Pengguna & Reset Password"),
        new(Security.RolesManage, "Security", "Kelola Role & Matriks Izin"),
        new(Security.AuditView, "Security", "Lihat Jejak Audit Lengkap"),

        // Dashboard
        new(Dashboard.ExecutiveView, "Dashboard", "Lihat KPI Keuangan & Omset"),

        // Master
        new(Master.CompanyManage, "Master", "Kelola Entitas Perusahaan dan Cabang"),
        new(Master.ProductManage, "Master", "Kelola Master Produk & Harga"),
        new(Master.CustomerManage, "Master", "Kelola Data Pelanggan & Limit Kredit"),
        new(Master.SupplierManage, "Master", "Kelola Data Pemasok/Vendor"),

        // Sales
        new(Sales.QuotationCreate, "Sales", "Buat & Kirim Penawaran Harga"),
        new(Sales.OrderCreate, "Sales", "Input Draf Pesanan Penjualan"),
        new(Sales.OrderApprove, "Sales", "Konfirmasi & Rilis Sales Order"),
        new(Sales.DeliveryDispatch, "Sales", "Cetak & Rilis Surat Jalan (DO)"),
        new(Sales.InvoiceIssue, "Sales", "Terbitkan Faktur Tagihan Penjualan"),

        // Procurement
        new(Procurement.PrCreate, "Procurement", "Buat Permintaan Pembelian (PR)"),
        new(Procurement.PrApprove, "Procurement", "Setujui Dokumen PR Departemen"),
        new(Procurement.PoCreate, "Procurement", "Terbitkan Pesanan Pembelian (PO)"),
        new(Procurement.PoApprove, "Procurement", "Otorisasi PO ke Vendor"),
        new(Procurement.GrnReceive, "Procurement", "Terima Barang Masuk dari PO (GRN)"),
        new(Procurement.BillVerify, "Procurement", "Verifikasi 3-Way & Tagihan Vendor"),

        // Inventory
        new(Inventory.StockView, "Inventory", "Lihat Saldo & Kartu Stok Gudang"),
        new(Inventory.TransferCreate, "Inventory", "Inisiasi Transfer Antar Gudang"),
        new(Inventory.AdjustmentApprove, "Inventory", "Setujui Selisih Stock Opname"),

        // Finance
        new(Finance.CoaManage, "Finance", "Kelola Bagan Akun Standar (COA)"),
        new(Finance.JournalCreate, "Finance", "Input Entri Jurnal Umum"),
        new(Finance.JournalPost, "Finance", "Posting Permanen Jurnal Buku Besar"),
        new(Finance.PaymentProcess, "Finance", "Eksekusi Pembayaran Kas & Bank"),
        new(Finance.PeriodClose, "Finance", "Kunci Periode Fiskal Bulanan"),

        // HR
        new(Hr.EmployeeManage, "HR", "Kelola Biodata Karyawan"),
        new(Hr.AttendanceManage, "HR", "Kelola Presensi & Lembur Karyawan"),
        new(Hr.LeaveApprove, "HR", "Persetujuan Permohonan Cuti Karyawan"),
        new(Hr.PayrollProcess, "HR", "Hitung & Posting Batch Payroll Bulanan"),
        new(Hr.SelfServiceView, "HR", "Akses Slip Gaji & Absensi Diri"),

        // Reports
        new(Reports.FinancialView, "Reports", "Buka Laporan Neraca & Laba Rugi"),
        new(Reports.ExportData, "Reports", "Ekspor Laporan ke Excel & PDF")
    ];
}

