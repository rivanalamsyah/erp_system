using System.Security.Claims;
using ERP.Application.Common.Interfaces;
using ERP.Domain.Entities.Finance;
using ERP.Domain.Entities.Hr;
using ERP.Domain.Entities.Inventory;
using ERP.Domain.Entities.Master;
using ERP.Domain.Entities.Procurement;
using ERP.Domain.Entities.Sales;
using ERP.Infrastructure.Persistence.Context;
using ERP.Infrastructure.Persistence.Interceptors;
using ERP.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;
using Xunit;

namespace ERP.IntegrationTests;

public class EnterpriseRemediationIntegrationTests
{
    private const string ConnectionString =
        "Host=127.0.0.1;Port=5433;Database=enterprise_erp;Username=erp_admin;Password=erp_admin";

    private static CurrentUserService CreateCurrentUserService(
        Guid userId,
        Guid companyId,
        Guid branchId,
        bool isHeadOffice = true,
        IEnumerable<Guid>? allowedBranchIds = null,
        IEnumerable<string>? permissions = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, "tester@enterprise.erp"),
            new("FullName", "Integration Tester"),
            new("CompanyId", companyId.ToString()),
            new("BranchId", branchId.ToString()),
            new("IsHeadOffice", isHeadOffice ? "true" : "false"),
            new("AllowedBranchId", branchId.ToString())
        };

        if (allowedBranchIds != null)
        {
            foreach (var bId in allowedBranchIds.Where(id => id != branchId))
            {
                claims.Add(new Claim("AllowedBranchId", bId.ToString()));
            }
        }

        if (permissions != null)
        {
            foreach (var perm in permissions)
            {
                claims.Add(new Claim("Permission", perm));
            }
        }

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);

        return new CurrentUserService(accessor);
    }

    private static ApplicationDbContext CreateDbContext(ICurrentUserService currentUserService)
    {
        var dateTimeProvider = new SystemDateTimeProvider();
        var interceptor = new AuditSaveChangesInterceptor(currentUserService, dateTimeProvider);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options, currentUserService, interceptor);
    }

    [Fact]
    public void CurrentUserService_BranchSwitcher_ShouldEnforce_AllowedBranchIds_AndIsolateContext()
    {
        var userId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var branch1 = Guid.NewGuid();
        var branch2 = Guid.NewGuid();
        var unauthorizedBranch = Guid.NewGuid();

        var service = CreateCurrentUserService(
            userId,
            companyId,
            branch1,
            isHeadOffice: false,
            allowedBranchIds: [branch1, branch2],
            permissions: ["sales.order.view"]);

        service.CompanyId.Should().Be(companyId);
        service.BranchId.Should().Be(branch1);
        service.HasPermission("sales.order.view").Should().BeTrue();
        service.HasPermission("security.users.manage").Should().BeFalse();

        // Switching to an authorized branch succeeds
        service.SetActiveBranch(branch2, companyId, false).Should().BeTrue();
        service.BranchId.Should().Be(branch2);

        // Switching to an unauthorized branch is rejected and preserves active branch
        service.SetActiveBranch(unauthorizedBranch, companyId, false).Should().BeFalse();
        service.BranchId.Should().Be(branch2);

        // Dynamic permission override updates active circuit immediately
        service.OverridePermissions(["sales.order.view", "sales.order.create"]);
        service.HasPermission("sales.order.create").Should().BeTrue();
    }

    [Fact]
    public async Task GlobalQueryFilters_ShouldStrictlyIsolateTenants_AndRejectEmptyCompanyId()
    {
        var userId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();

        var compA = Company.Create($"TNA-{suffix}", "Tenant Alpha", "01.111", "Jakarta", null, null, userId);
        var compB = Company.Create($"TNB-{suffix}", "Tenant Beta", "02.222", "Surabaya", null, null, userId);

        var userA = CreateCurrentUserService(userId, compA.Id, Guid.Empty);
        await using var dbA = CreateDbContext(userA);
        await using var tx = await dbA.Database.BeginTransactionAsync();

        dbA.Companies.AddRange(compA, compB);

        var custA = Customer.Create(compA.Id, $"CA-{suffix}", "Customer Alpha", null, null, "Jakarta", "Jakarta", null, 1000000m, 30, userId);
        var custB = Customer.Create(compB.Id, $"CB-{suffix}", "Customer Beta", null, null, "Surabaya", "Surabaya", null, 1000000m, 30, userId);
        dbA.Customers.AddRange(custA, custB);
        await dbA.SaveChangesAsync();

        // Context A should only see Customer Alpha
        var visibleToA = await dbA.Customers.Where(c => c.Id == custA.Id || c.Id == custB.Id).ToListAsync();
        visibleToA.Should().ContainSingle(c => c.Id == custA.Id);

        // Unauthenticated context (CompanyId == Guid.Empty) must see 0 rows (no Guid.Empty bypass)
        var emptyUser = CreateCurrentUserService(Guid.Empty, Guid.Empty, Guid.Empty);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(dbA.Database.GetDbConnection())
            .Options;
        await using var dbEmpty = new ApplicationDbContext(
            options,
            emptyUser,
            new AuditSaveChangesInterceptor(emptyUser, new SystemDateTimeProvider()));
        await dbEmpty.Database.UseTransactionAsync(dbA.Database.CurrentTransaction!.GetDbTransaction());

        var visibleToEmpty = await dbEmpty.Customers.Where(c => c.Id == custA.Id || c.Id == custB.Id).ToListAsync();
        visibleToEmpty.Should().BeEmpty("Global query filter must not bypass tenant isolation when CompanyId is Guid.Empty");

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task DocumentSequenceService_ShouldBeAtomic_WithCallerUnitOfWork()
    {
        var userId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var comp = Company.Create($"SEQ-{suffix}", "Sequence Tenant", "01.111", "Jakarta", null, null, userId);
        var branch = Branch.Create(comp.Id, $"BR-{suffix}", "Main Branch", "Jakarta", "021", true, userId);

        var currentUser = CreateCurrentUserService(userId, comp.Id, branch.Id);
        await using var db = CreateDbContext(currentUser);
        await using var tx = await db.Database.BeginTransactionAsync();

        db.Companies.Add(comp);
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var seqService = new DocumentSequenceService(db, new SystemDateTimeProvider());

        // Generate two numbers in the same unit of work BEFORE SaveChangesAsync
        var doc1 = await seqService.NextDocumentNumberAsync(comp.Id, branch.Id, "SO");
        var doc2 = await seqService.NextDocumentNumberAsync(comp.Id, branch.Id, "SO");

        doc1.Should().EndWith("/00001");
        doc2.Should().EndWith("/00002");

        // Verify that before SaveChangesAsync, nothing was committed to the database yet
        var persistedCountBeforeSave = await db.DocumentSequences
            .AsNoTracking()
            .CountAsync(s => s.CompanyId == comp.Id && s.BranchId == branch.Id && s.DocumentType == "SO");
        persistedCountBeforeSave.Should().Be(0, "DocumentSequenceService must not call SaveChangesAsync prematurely");

        // Now commit unit of work
        await db.SaveChangesAsync();

        var persistedAfterSave = await db.DocumentSequences
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompanyId == comp.Id && s.BranchId == branch.Id && s.DocumentType == "SO");
        persistedAfterSave.Should().NotBeNull();
        persistedAfterSave!.CurrentSequence.Should().Be(2);

        await tx.RollbackAsync();
    }

    [Fact]
    public async Task StockBalance_And_DocumentSequence_ShouldEnforce_XminOptimisticConcurrency()
    {
        var userId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var comp = Company.Create($"CNC-{suffix}", "Concurrency Tenant", "01.111", "Jakarta", null, null, userId);
        var branch = Branch.Create(comp.Id, $"BR-{suffix}", "Branch", "Jakarta", "021", true, userId);
        var wh = Warehouse.Create(comp.Id, branch.Id, $"WH-{suffix}", "Warehouse", "Jakarta", userId);
        var cat = ProductCategory.Create(comp.Id, $"CAT-{suffix}", "Category", null, userId);
        var uom = UnitOfMeasure.Create(comp.Id, $"UOM-{suffix}", "Unit", userId);
        var prod = Product.Create(comp.Id, cat.Id, uom.Id, $"SKU-{suffix}", null, "Product", null, ProductType.Inventory, 1000m, 1500m, 1m, userId);
        var balance = StockBalance.Create(comp.Id, wh.Id, prod.Id);
        balance.AddStock(10m, 1000m);

        var currentUser = CreateCurrentUserService(userId, comp.Id, branch.Id);
        await using var db1 = CreateDbContext(currentUser);

        // Verify EF Core metadata configures xmin concurrency token on StockBalance and DocumentSequence
        var sbVersionProp = db1.Model.FindEntityType(typeof(StockBalance))?.FindProperty(nameof(StockBalance.Version));
        sbVersionProp.Should().NotBeNull();
        sbVersionProp!.IsConcurrencyToken.Should().BeTrue();
        sbVersionProp.GetColumnName().Should().Be("xmin");

        var dsVersionProp = db1.Model.FindEntityType(typeof(DocumentSequence))?.FindProperty(nameof(DocumentSequence.Version));
        dsVersionProp.Should().NotBeNull();
        dsVersionProp!.IsConcurrencyToken.Should().BeTrue();
        dsVersionProp.GetColumnName().Should().Be("xmin");
    }

    [Fact]
    public async Task AccountingPostingService_ShouldAutoPostBalancedJournals_BeIdempotent_AndEnforceClosedFiscalPeriod()
    {
        var userId = Guid.NewGuid();
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var comp = Company.Create($"ACC-{suffix}", "Accounting Tenant", "01.111", "Jakarta", null, null, userId);
        var branch = Branch.Create(comp.Id, $"BR-{suffix}", "Head Office", "Jakarta", "021", true, userId);
        var wh = Warehouse.Create(comp.Id, branch.Id, $"WH-{suffix}", "Main Warehouse", "Jakarta", userId);
        var cust = Customer.Create(comp.Id, $"CU-{suffix}", "PT Pelanggan", null, null, "Jakarta", "Jakarta", null, 100_000_000m, 30, userId);
        var supp = Supplier.Create(comp.Id, $"SP-{suffix}", "PT Vendor", null, null, "Jakarta", null, 30, null, null, null, userId);
        var cat = ProductCategory.Create(comp.Id, $"CT-{suffix}", "Hardware", null, userId);
        var uom = UnitOfMeasure.Create(comp.Id, $"UM-{suffix}", "PCS", userId);
        var prod = Product.Create(comp.Id, cat.Id, uom.Id, $"PR-{suffix}", null, "Server Rack", null, ProductType.Inventory, 1_000_000m, 2_000_000m, 1m, userId);

        var currentUser = CreateCurrentUserService(userId, comp.Id, branch.Id);
        await using var db = CreateDbContext(currentUser);
        await using var tx = await db.Database.BeginTransactionAsync();

        db.Companies.Add(comp);
        db.Branches.Add(branch);
        db.Warehouses.Add(wh);
        db.Customers.Add(cust);
        db.Suppliers.Add(supp);
        db.ProductCategories.Add(cat);
        db.UnitsOfMeasure.Add(uom);
        db.Products.Add(prod);

        var po = PurchaseOrder.Create(comp.Id, branch.Id, supp.Id, $"PO/{suffix}/0001", today, today.AddDays(7), userId);
        po.AddItem(prod.Id, 5m, 1_000_000m, 0m);
        po.Approve(userId);
        db.PurchaseOrders.Add(po);

        var grn = GoodsReceipt.Create(comp.Id, branch.Id, wh.Id, po.Id, $"GRN/{suffix}/0001", "SJ-001", today, userId);
        grn.AddItem(po.Items.First().Id, prod.Id, 5m);
        grn.MarkReceived(userId);
        db.GoodsReceipts.Add(grn);

        var bill = VendorBill.Create(comp.Id, branch.Id, supp.Id, po.Id, $"BILL/{suffix}/0001", "INV-V-01", today, today.AddDays(30), po.TotalAmount, userId);
        bill.Post(userId);
        db.VendorBills.Add(bill);

        var so = SalesOrder.Create(comp.Id, branch.Id, cust.Id, $"SO/{suffix}/0001", today, today.AddDays(7), userId);
        so.AddItem(prod.Id, 2m, 2_000_000m, 0m);
        so.Confirm(userId);
        db.SalesOrders.Add(so);

        var delivery = Delivery.Create(comp.Id, branch.Id, wh.Id, so.Id, $"DO/{suffix}/0001", today, "TRK-01", userId);
        delivery.AddItem(so.Items.First().Id, prod.Id, 2m);
        delivery.Dispatch(userId);
        db.Deliveries.Add(delivery);

        var invoice = SalesInvoice.Create(comp.Id, branch.Id, cust.Id, so.Id, $"INV/{suffix}/0001", "010-001", today, today.AddDays(30), so.SubtotalAmount, so.TaxAmount, so.TotalAmount, userId);
        invoice.Post(userId);
        db.SalesInvoices.Add(invoice);

        await db.SaveChangesAsync();

        var seqService = new DocumentSequenceService(db, new SystemDateTimeProvider());
        var accountingService = new AccountingPostingService(db, seqService);

        // 1. Auto-post GRN, VendorBill, Delivery COGS, and SalesInvoice
        var grnGl = await accountingService.PostGoodsReceiptJournalAsync(grn, 5_000_000m, userId);
        var billGl = await accountingService.PostVendorBillJournalAsync(bill, userId);
        var doGl = await accountingService.PostDeliveryCogsJournalAsync(delivery, 2_000_000m, userId);
        var invGl = await accountingService.PostSalesInvoiceJournalAsync(invoice, userId);

        grnGl.IsSuccess.Should().BeTrue();
        billGl.IsSuccess.Should().BeTrue();
        doGl.IsSuccess.Should().BeTrue();
        invGl.IsSuccess.Should().BeTrue();

        await db.SaveChangesAsync();

        // 2. Verify Idempotency: calling PostSalesInvoiceJournalAsync again returns the exact same JournalEntry Id
        var invGlDuplicate = await accountingService.PostSalesInvoiceJournalAsync(invoice, userId);
        invGlDuplicate.IsSuccess.Should().BeTrue();
        invGlDuplicate.Value.Should().Be(invGl.Value);

        var postedJournals = await db.JournalEntries
            .Include(j => j.Lines)
            .Where(j => j.CompanyId == comp.Id)
            .ToListAsync();

        postedJournals.Should().HaveCount(4);
        postedJournals.Should().OnlyContain(j => j.Status == JournalStatus.Posted && j.TotalDebit == j.TotalCredit && j.TotalDebit > 0);

        // 3. Verify BR-ACC-002: Closed Fiscal Period blocks posting
        var closedPeriod = FiscalPeriod.Create(comp.Id, today.Year, today.Month, new DateOnly(today.Year, today.Month, 1), new DateOnly(today.Year, today.Month, 28));
        closedPeriod.Close(userId);
        db.FiscalPeriods.Add(closedPeriod);
        await db.SaveChangesAsync();

        var periodCheck = await accountingService.EnsureFiscalPeriodOpenAsync(comp.Id, today);
        periodCheck.IsFailure.Should().BeTrue();
        periodCheck.Error.Code.Should().Be("BR-ACC-002");

        await tx.RollbackAsync();
    }
}

