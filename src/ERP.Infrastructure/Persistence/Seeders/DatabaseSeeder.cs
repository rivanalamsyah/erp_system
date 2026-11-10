using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Context;
using ERP.Shared.Security;

namespace ERP.Infrastructure.Persistence.Seeders;

public class DatabaseSeeder(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public static readonly Guid DefaultCompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DefaultBranchId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public async Task SeedAsync()
    {
        logger.LogInformation("Starting database seeding...");

        await SeedMasterDataAsync();
        await SeedPermissionsAsync();
        await SeedRolesAndRolePermissionsAsync();
        await SeedAdminUserAsync();
        await SeedInventoryAsync();
        await SeedFinanceBaselineAsync();

        logger.LogInformation("Database seeding completed successfully.");
    }

    private async Task SeedMasterDataAsync()
    {
        // 1. Company
        var company = await context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(c => c.Id == DefaultCompanyId);
        if (company == null)
        {
            company = Company.Create(
                code: "CMP-001",
                name: "PT Enterprise Sinergi Nusantara",
                taxIdentificationNumber: "01.234.567.8-901.000",
                address: "Jl. Jenderal Sudirman Kav. 52-53, Jakarta Selatan",
                phone: "+62 21 555 1234",
                email: "info@enterprise.erp",
                createdByUserId: SystemUserId,
                currencyCode: "IDR");

            typeof(Company).GetProperty(nameof(Company.Id))!.SetValue(company, DefaultCompanyId);
            context.Companies.Add(company);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default company: {CompanyName}", company.Name);
        }

        // 2. Branch
        var branch = await context.Branches.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.Id == DefaultBranchId);
        if (branch == null)
        {
            branch = Branch.Create(
                companyId: DefaultCompanyId,
                code: "HO-JKT",
                name: "Head Office Jakarta",
                address: "Jl. Jenderal Sudirman Kav. 52-53, Lt. 18, Jakarta",
                phone: "+62 21 555 1234",
                isHeadOffice: true,
                createdByUserId: SystemUserId);

            typeof(Branch).GetProperty(nameof(Branch.Id))!.SetValue(branch, DefaultBranchId);
            context.Branches.Add(branch);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default branch: {BranchName}", branch.Name);
        }

        // 3. Departments
        if (!await context.Departments.IgnoreQueryFilters().AnyAsync(d => d.CompanyId == DefaultCompanyId))
        {
            var depts = new[]
            {
                Department.Create(DefaultCompanyId, "IT", "Information Technology", SystemUserId),
                Department.Create(DefaultCompanyId, "FIN", "Finance & Accounting", SystemUserId),
                Department.Create(DefaultCompanyId, "SLS", "Sales & Marketing", SystemUserId),
                Department.Create(DefaultCompanyId, "PRC", "Procurement & Supply Chain", SystemUserId),
                Department.Create(DefaultCompanyId, "WHS", "Warehouse & Inventory", SystemUserId),
                Department.Create(DefaultCompanyId, "HR", "Human Resources", SystemUserId)
            };
            context.Departments.AddRange(depts);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded master departments.");
        }

        // 4. Units of Measure
        if (!await context.UnitsOfMeasure.IgnoreQueryFilters().AnyAsync(u => u.CompanyId == DefaultCompanyId))
        {
            var uoms = new[]
            {
                UnitOfMeasure.Create(DefaultCompanyId, "PCS", "Pieces", SystemUserId),
                UnitOfMeasure.Create(DefaultCompanyId, "BOX", "Box / Dus", SystemUserId),
                UnitOfMeasure.Create(DefaultCompanyId, "KG", "Kilogram", SystemUserId),
                UnitOfMeasure.Create(DefaultCompanyId, "LTR", "Liter", SystemUserId)
            };
            context.UnitsOfMeasure.AddRange(uoms);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded units of measure.");
        }

        // 5. Product Categories
        if (!await context.ProductCategories.IgnoreQueryFilters().AnyAsync(c => c.CompanyId == DefaultCompanyId))
        {
            var categories = new[]
            {
                ProductCategory.Create(DefaultCompanyId, "RAW", "Bahan Baku (Raw Material)", null, SystemUserId),
                ProductCategory.Create(DefaultCompanyId, "FG", "Barang Jadi (Finished Goods)", null, SystemUserId),
                ProductCategory.Create(DefaultCompanyId, "SRV", "Jasa & Layanan (Services)", null, SystemUserId)
            };
            context.ProductCategories.AddRange(categories);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded product categories.");
        }
    }

    private async Task SeedInventoryAsync()
    {
        var warehouse = await context.Warehouses.IgnoreQueryFilters().FirstOrDefaultAsync(w => w.CompanyId == DefaultCompanyId);
        if (warehouse == null)
        {
            warehouse = Warehouse.Create(
                companyId: DefaultCompanyId,
                branchId: DefaultBranchId,
                code: "WH-JKT-01",
                name: "Gudang Utama Jakarta",
                location: "Kawasan Industri Pulo Gadung Blok B No. 12, Jakarta Timur",
                createdByUserId: SystemUserId);

            context.Warehouses.Add(warehouse);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded default central warehouse: {WarehouseName}", warehouse.Name);
        }
    }

    private async Task SeedPermissionsAsync()
    {
        var existingPermCodes = await context.Permissions.Select(p => p.Code).ToListAsync();
        var missingPerms = Permissions.All
            .Where(p => !existingPermCodes.Contains(p.Code))
            .Select(p => new Permission
            {
                Id = Guid.NewGuid(),
                Code = p.Code,
                Module = p.Module,
                Description = p.Description,
                CreatedAtUtc = DateTime.UtcNow
            })
            .ToList();

        if (missingPerms.Count > 0)
        {
            context.Permissions.AddRange(missingPerms);
            await context.SaveChangesAsync();
            logger.LogInformation("Seeded {Count} system permissions.", missingPerms.Count);
        }
    }

    private async Task SeedRolesAndRolePermissionsAsync()
    {
        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole
                {
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    Description = $"System ERP Role: {roleName}",
                    IsSystemRole = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                await roleManager.CreateAsync(role);
                logger.LogInformation("Seeded role: {RoleName}", roleName);
            }
        }

        // Map Permissions to Roles
        var allPerms = await context.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);
        var allRoles = await roleManager.Roles.ToDictionaryAsync(r => r.Name!, r => r.Id);

        var matrix = new Dictionary<string, string[]>
        {
            [Roles.SuperAdmin] = Permissions.All.Select(p => p.Code).ToArray(),
            [Roles.Director] =
            [
                Permissions.Security.AuditView,
                Permissions.Dashboard.ExecutiveView,
                Permissions.Sales.OrderApprove,
                Permissions.Procurement.PrApprove,
                Permissions.Procurement.PoApprove,
                Permissions.Inventory.StockView,
                Permissions.Inventory.AdjustmentApprove,
                Permissions.Finance.PeriodClose,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.FinancialView,
                Permissions.Reports.ExportData
            ],
            [Roles.FinanceManager] =
            [
                Permissions.Dashboard.ExecutiveView,
                Permissions.Master.CustomerManage,
                Permissions.Master.SupplierManage,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.PrApprove,
                Permissions.Procurement.PoApprove,
                Permissions.Procurement.BillVerify,
                Permissions.Inventory.StockView,
                Permissions.Inventory.AdjustmentApprove,
                Permissions.Finance.CoaManage,
                Permissions.Finance.JournalCreate,
                Permissions.Finance.JournalPost,
                Permissions.Finance.PaymentProcess,
                Permissions.Finance.PeriodClose,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.PayrollProcess,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.FinancialView,
                Permissions.Reports.ExportData
            ],
            [Roles.FinanceStaff] =
            [
                Permissions.Sales.InvoiceIssue,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.BillVerify,
                Permissions.Inventory.StockView,
                Permissions.Finance.JournalCreate,
                Permissions.Finance.PaymentProcess,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.SalesManager] =
            [
                Permissions.Dashboard.ExecutiveView,
                Permissions.Master.CustomerManage,
                Permissions.Sales.QuotationCreate,
                Permissions.Sales.OrderCreate,
                Permissions.Sales.OrderApprove,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.PrApprove,
                Permissions.Inventory.StockView,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.SalesExecutive] =
            [
                Permissions.Master.CustomerManage,
                Permissions.Sales.QuotationCreate,
                Permissions.Sales.OrderCreate,
                Permissions.Procurement.PrCreate,
                Permissions.Inventory.StockView,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.ProcurementManager] =
            [
                Permissions.Master.ProductManage,
                Permissions.Master.SupplierManage,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.PrApprove,
                Permissions.Procurement.PoCreate,
                Permissions.Procurement.PoApprove,
                Permissions.Inventory.StockView,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.WarehouseManager] =
            [
                Permissions.Master.ProductManage,
                Permissions.Sales.DeliveryDispatch,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.PrApprove,
                Permissions.Procurement.GrnReceive,
                Permissions.Inventory.StockView,
                Permissions.Inventory.TransferCreate,
                Permissions.Inventory.AdjustmentApprove,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.WarehouseStaff] =
            [
                Permissions.Sales.DeliveryDispatch,
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.GrnReceive,
                Permissions.Inventory.StockView,
                Permissions.Inventory.TransferCreate,
                Permissions.Hr.SelfServiceView
            ],
            [Roles.HrManager] =
            [
                Permissions.Procurement.PrCreate,
                Permissions.Procurement.PrApprove,
                Permissions.Hr.EmployeeManage,
                Permissions.Hr.AttendanceManage,
                Permissions.Hr.LeaveApprove,
                Permissions.Hr.PayrollProcess,
                Permissions.Hr.SelfServiceView,
                Permissions.Reports.ExportData
            ],
            [Roles.Employee] =
            [
                Permissions.Hr.SelfServiceView
            ]
        };

        foreach (var (roleKey, permCodes) in matrix)
        {
            if (!allRoles.TryGetValue(roleKey, out var roleId)) continue;

            var existingRolePermIds = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var toAdd = new List<RolePermission>();
            foreach (var code in permCodes)
            {
                if (allPerms.TryGetValue(code, out var permId) && !existingRolePermIds.Contains(permId))
                {
                    toAdd.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permId,
                        GrantedAtUtc = DateTime.UtcNow
                    });
                }
            }

            if (toAdd.Count > 0)
            {
                context.RolePermissions.AddRange(toAdd);
                await context.SaveChangesAsync();
                logger.LogInformation("Mapped {Count} permissions to role {RoleName}.", toAdd.Count, roleKey);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var adminEmail = configuration["SUPERADMIN_EMAIL"] ?? "admin@enterprise.erp";
        var adminPassword = configuration["SUPERADMIN_PASSWORD"] ?? "Admin@12345";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);

        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "System Super Administrator",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (createResult.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, Roles.SuperAdmin);

                // Assign default branch
                var userBranch = new UserBranch
                {
                    UserId = adminUser.Id,
                    BranchId = DefaultBranchId,
                    IsDefault = true
                };
                context.UserBranches.Add(userBranch);
                await context.SaveChangesAsync();

                logger.LogInformation("Super Admin user '{Email}' seeded successfully.", adminEmail);
            }
            else
            {
                logger.LogError("Failed to create admin user: {Errors}",
                    string.Join(", ", createResult.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedFinanceBaselineAsync()
    {
        // 1. Chart of Accounts (COA)
        if (!await context.ChartOfAccounts.IgnoreQueryFilters().AnyAsync(c => c.CompanyId == DefaultCompanyId))
        {
            var coaList = new List<ERP.Domain.Entities.Finance.ChartOfAccount>
            {
                // Assets
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1000", "Aset Lancar (Current Assets)", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId, null, true),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1100", "Kas Operasional", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1200", "Bank BCA Giro Operasional", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1300", "Piutang Usaha (AR)", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1400", "Persediaan Barang Dagang", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "1-1500", "PPN Masukan (Prepaid VAT)", ERP.Domain.Entities.Finance.AccountType.Asset, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),

                // Liabilities
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "2-1000", "Kewajiban Lancar (Current Liabilities)", ERP.Domain.Entities.Finance.AccountType.Liability, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId, null, true),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "2-1100", "Hutang Usaha (AP)", ERP.Domain.Entities.Finance.AccountType.Liability, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "2-1200", "Hutang Gaji & Upah Karyawan", ERP.Domain.Entities.Finance.AccountType.Liability, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "2-1300", "PPN Keluaran (VAT Output)", ERP.Domain.Entities.Finance.AccountType.Liability, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),

                // Equity
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "3-1000", "Modal Saham Disetor", ERP.Domain.Entities.Finance.AccountType.Equity, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "3-2000", "Laba Ditahan (Retained Earnings)", ERP.Domain.Entities.Finance.AccountType.Equity, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),

                // Revenue
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "4-1000", "Pendapatan Penjualan Produk", ERP.Domain.Entities.Finance.AccountType.Revenue, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "4-2000", "Pendapatan Jasa & Servis", ERP.Domain.Entities.Finance.AccountType.Revenue, ERP.Domain.Entities.Finance.NormalBalance.CR, SystemUserId),

                // Expense / COGS
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "5-1000", "Beban Pokok Penjualan (HPP / COGS)", ERP.Domain.Entities.Finance.AccountType.Expense, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "6-1000", "Beban Operasional & Administrasi", ERP.Domain.Entities.Finance.AccountType.Expense, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId, null, true),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "6-1100", "Beban Gaji, Tunjangan & Upah", ERP.Domain.Entities.Finance.AccountType.Expense, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "6-1200", "Beban Utilitas, Listrik & Air", ERP.Domain.Entities.Finance.AccountType.Expense, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId),
                ERP.Domain.Entities.Finance.ChartOfAccount.Create(DefaultCompanyId, "6-1300", "Beban Sewa & Pemeliharaan Kantor", ERP.Domain.Entities.Finance.AccountType.Expense, ERP.Domain.Entities.Finance.NormalBalance.DR, SystemUserId)
            };

            context.ChartOfAccounts.AddRange(coaList);
            await context.SaveChangesAsync();

            // 2. Default Cash & Bank Accounts
            var kasAccount = coaList.First(c => c.AccountCode == "1-1100");
            var bankAccount = coaList.First(c => c.AccountCode == "1-1200");

            var kas = ERP.Domain.Entities.Finance.CashBankAccount.Create(
                DefaultCompanyId,
                DefaultBranchId,
                kasAccount.Id,
                "KAS-JKT",
                "Kas Kecil Head Office",
                null,
                null,
                SystemUserId);

            var bank = ERP.Domain.Entities.Finance.CashBankAccount.Create(
                DefaultCompanyId,
                DefaultBranchId,
                bankAccount.Id,
                "BCA-001",
                "BCA Giro Operasional Rupiah",
                "Bank Central Asia",
                "8830-192-881",
                SystemUserId);

            context.CashBankAccounts.AddRange(kas, bank);

            // 3. Fiscal Periods for 2026
            var currentYear = DateTime.UtcNow.Year;
            for (int month = 1; month <= 12; month++)
            {
                var startDate = new DateOnly(currentYear, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var period = ERP.Domain.Entities.Finance.FiscalPeriod.Create(
                    DefaultCompanyId,
                    currentYear,
                    month,
                    startDate,
                    endDate);

                context.FiscalPeriods.Add(period);
            }

            await context.SaveChangesAsync();
            logger.LogInformation("Finance baseline (COA, CashBank, FiscalPeriods) seeded successfully.");
        }
    }
}

