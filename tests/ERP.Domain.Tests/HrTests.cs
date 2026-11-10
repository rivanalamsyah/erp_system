using ERP.Domain.Entities.Hr;
using FluentAssertions;
using Xunit;

namespace ERP.Domain.Tests;

public class HrTests
{
    [Fact]
    public void Employee_Create_ValidParams_ShouldSucceed()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var emp = Employee.Create(
            companyId,
            branchId,
            departmentId,
            "EMP-001",
            "Budi Santoso",
            "3171012345678901",
            new DateOnly(1990, 5, 20),
            new DateOnly(2020, 1, 10),
            EmploymentStatus.Permanent,
            8000000m,
            userId
        );

        emp.Should().NotBeNull();
        emp.EmployeeNumber.Should().Be("EMP-001");
        emp.FullName.Should().Be("Budi Santoso");
        emp.BasicSalary.Should().Be(8000000m);
        emp.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Employee_Create_NegativeSalary_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => Employee.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EMP-002",
            "Invalid Salary",
            "3171012345678902",
            new DateOnly(1992, 1, 1),
            new DateOnly(2021, 1, 1),
            EmploymentStatus.Permanent,
            -100m,
            Guid.NewGuid()
        );

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Gaji pokok tidak boleh negatif*");
    }

    [Fact]
    public void LeaveRequest_Create_ZeroOrNegativeDays_ShouldThrowArgumentOutOfRangeException()
    {
        var act = () => LeaveRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateOnly.FromDateTime(DateTime.Today),
            DateOnly.FromDateTime(DateTime.Today),
            0,
            "Alasan Cuti"
        );

        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("*Jumlah hari cuti harus lebih besar dari 0*");
    }

    [Fact]
    public void PayrollPeriod_AddItem_NegativeNetSalary_ShouldEnforce_BR_HR_002()
    {
        var period = PayrollPeriod.Create(
            Guid.NewGuid(),
            "Payroll Mei 2026",
            new DateOnly(2026, 5, 1),
            new DateOnly(2026, 5, 31),
            Guid.NewGuid()
        );

        // Basic: 5,000,000, Allowances: 0, Deductions: 4,000,000, PPh21: 2,000,000 -> Net: -1,000,000
        var act = () => period.AddItem(
            Guid.NewGuid(),
            5000000m,
            0m,
            4000000m,
            2000000m
        );

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BR-HR-002*Gaji bersih tidak boleh negatif*");
    }

    [Fact]
    public void PayrollPeriod_ApproveAndPay_ShouldUpdateStatusAndMarkItemsPaid()
    {
        var period = PayrollPeriod.Create(
            Guid.NewGuid(),
            "Payroll Juni 2026",
            new DateOnly(2026, 6, 1),
            new DateOnly(2026, 6, 30),
            Guid.NewGuid()
        );

        period.AddItem(Guid.NewGuid(), 6000000m, 500000m, 100000m, 50000m);
        period.Items.Should().HaveCount(1);
        period.Items.First().NetSalary.Should().Be(6350000m);
        period.Items.First().IsPaid.Should().BeFalse();

        var approverId = Guid.NewGuid();
        period.Approve(approverId);
        period.Status.Should().Be(PayrollStatus.Approved);

        period.MarkPaid();
        period.Status.Should().Be(PayrollStatus.Paid);
        period.Items.First().IsPaid.Should().BeTrue();
    }
}

