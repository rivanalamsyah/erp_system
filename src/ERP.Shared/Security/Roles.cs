namespace ERP.Shared.Security;

public static class Roles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string Director = "DIRECTOR";
    public const string FinanceManager = "FIN_MGR";
    public const string FinanceStaff = "FIN_STAFF";
    public const string SalesManager = "SALES_MGR";
    public const string SalesExecutive = "SALES_EXEC";
    public const string ProcurementManager = "PROC_MGR";
    public const string WarehouseManager = "WHS_MGR";
    public const string WarehouseStaff = "WHS_STAFF";
    public const string HrManager = "HR_MGR";
    public const string Employee = "EMPLOYEE";

    public static readonly IReadOnlyList<string> All =
    [
        SuperAdmin,
        Director,
        FinanceManager,
        FinanceStaff,
        SalesManager,
        SalesExecutive,
        ProcurementManager,
        WarehouseManager,
        WarehouseStaff,
        HrManager,
        Employee
    ];
}

