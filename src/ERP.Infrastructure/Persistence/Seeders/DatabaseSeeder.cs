using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ERP.Domain.Entities.Master;
using ERP.Infrastructure.Identity.Models;
using ERP.Infrastructure.Persistence.Context;
using ERP.Shared.Security;

namespace ERP.Infrastructure.Persistence.Seeders;

public class DatabaseSeeder(
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
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
        const string adminEmail = "admin@enterprise.erp";
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

            var createResult = await userManager.CreateAsync(adminUser, "Admin@12345");
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
}

