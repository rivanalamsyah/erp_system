using ERP.Domain.Entities.Finance;
using ERP.Domain.Entities.Hr;
using FluentAssertions;
using Xunit;

namespace ERP.IntegrationTests;

public class FinancialAndHrIntegrationTests
{
    [Fact]
    public void Accounting_BalancedJournalPosting_ShouldSucceed()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var bankAccountId = Guid.NewGuid();
        var revenueAccountId = Guid.NewGuid();

        var entry = JournalEntry.Create(
            companyId,
            branchId,
            "JV/202611/0001",
            DateOnly.FromDateTime(DateTime.Today),
            "Sales",
            "INV/202611/0001",
            "Penerimaan Pelunasan Faktur Pelanggan",
            userId
        );

        // Debit Bank, Credit Revenue
        entry.AddDebitLine(bankAccountId, 10000000m, "Debit Kas/Bank Mandiri");
        entry.AddCreditLine(revenueAccountId, 10000000m, "Kredit Pendapatan Usaha");

        entry.TotalDebit.Should().Be(10000000m);
        entry.TotalCredit.Should().Be(10000000m);

        // BR-ACC-001: Zero Imbalance Invariant
        entry.Post(userId);
        entry.Status.Should().Be(JournalStatus.Posted);
    }

    [Fact]
    public void Accounting_UnbalancedJournalPosting_ShouldEnforce_BR_ACC_001()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var entry = JournalEntry.Create(
            companyId,
            branchId,
            "JV/202611/0002",
            DateOnly.FromDateTime(DateTime.Today),
            "Manual",
            "",
            "Jurnal Penyesuaian Tidak Seimbang",
            userId
        );

        entry.AddDebitLine(Guid.NewGuid(), 5000000m, "Debit Kas");
        entry.AddCreditLine(Guid.NewGuid(), 4000000m, "Kredit Biaya (Selisih 1 jt)");

        var act = () => entry.Post(userId);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BR-ACC-001*dilanggar*");
    }

    [Fact]
    public void HrAndPayroll_EndToEndLifecycle_ShouldEnforceRulesAndCalculateProperly()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // 1. Employee Registration
        var employee = Employee.Create(
            companyId,
            branchId,
            departmentId,
            "EMP-2026-001",
            "Ahmad Fauzi",
            "3171012345678901",
            new DateOnly(1992, 4, 15),
            new DateOnly(2021, 1, 1),
            EmploymentStatus.Permanent,
            10000000m, // Basic: 10,000,000 IDR
            userId
        );
        employee.IsActive.Should().BeTrue();

        // 2. Attendance Recording
        var attendance = AttendanceRecord.Create(
            employee.Id,
            DateOnly.FromDateTime(DateTime.Today),
            DateTime.UtcNow.Date.AddHours(8),
            DateTime.UtcNow.Date.AddHours(17),
            AttendanceStatus.Present,
            0,
            "Tepat waktu"
        );
        attendance.Status.Should().Be(AttendanceStatus.Present);

        // 3. Leave Management & BR-HR-001
        var leaveType = LeaveType.Create(companyId, "Cuti Tahunan", 12, userId);
        leaveType.AnnualAllowanceDays.Should().Be(12);

        var leave = LeaveRequest.Create(
            employee.Id,
            leaveType.Id,
            DateOnly.FromDateTime(DateTime.Today.AddDays(7)),
            DateOnly.FromDateTime(DateTime.Today.AddDays(9)),
            3,
            "Liburan keluarga tahunan"
        );
        leave.Status.Should().Be(LeaveRequestStatus.Pending);

        leave.Approve(userId);
        leave.Status.Should().Be(LeaveRequestStatus.Approved);

        // 4. Monthly Payroll Calculation & BR-HR-002
        var payrollPeriod = PayrollPeriod.Create(
            companyId,
            "Payroll November 2026",
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 30),
            userId
        );

        // Basic: 10,000,000, Allowances: 1,500,000, Deductions (BPJS): 200,000, PPh 21: 275,000
        var basic = 10000000m;
        var allowances = 1500000m;
        var deductions = 200000m;
        var pph21 = 275000m;

        payrollPeriod.AddItem(employee.Id, basic, allowances, deductions, pph21);

        var item = payrollPeriod.Items.First();
        item.BasicSalary.Should().Be(10000000m);
        item.Allowances.Should().Be(1500000m);
        item.NetSalary.Should().Be(11025000m); // 10,000,000 + 1,500,000 - 200,000 - 275,000
        item.IsPaid.Should().BeFalse();

        // 5. Approval & Payment
        payrollPeriod.Approve(userId);
        payrollPeriod.Status.Should().Be(PayrollStatus.Approved);

        payrollPeriod.MarkPaid();
        payrollPeriod.Status.Should().Be(PayrollStatus.Paid);
        item.IsPaid.Should().BeTrue();
    }
}
