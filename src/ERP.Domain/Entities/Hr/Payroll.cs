using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Hr;

public enum PayrollStatus
{
    Draft,
    Calculated,
    Approved,
    Paid
}

public class PayrollPeriod : BaseEntity<Guid>
{
    public string PeriodName { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public PayrollStatus Status { get; private set; } = PayrollStatus.Draft;
    public Guid? ApprovedByUserId { get; private set; }

    public Company Company { get; private set; } = default!;
    public ICollection<PayrollItem> Items { get; private set; } = [];

    private PayrollPeriod() { }

    public static PayrollPeriod Create(
        Guid companyId,
        string periodName,
        DateOnly startDate,
        DateOnly endDate,
        Guid createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(periodName);

        return new PayrollPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            PeriodName = periodName.Trim(),
            StartDate = startDate,
            EndDate = endDate,
            Status = PayrollStatus.Draft,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void AddItem(
        Guid employeeId,
        decimal basicSalary,
        decimal allowances,
        decimal deductions,
        decimal incomeTaxPph21)
    {
        // BR-HR-002: Gaji Bersih Tidak Boleh Negatif (Non-Negative Net Salary)
        var grossSalary = basicSalary + allowances;
        var totalDeductions = deductions + incomeTaxPph21;
        var netSalary = grossSalary - totalDeductions;

        if (netSalary < 0)
        {
            throw new InvalidOperationException(
                $"BR-HR-002: Pelanggaran perhitungan gaji. Gaji bersih tidak boleh negatif. Bruto (Rp {grossSalary:N2}) < Total Potongan (Rp {totalDeductions:N2}).");
        }

        Items.Add(new PayrollItem
        {
            Id = Guid.NewGuid(),
            PeriodId = Id,
            EmployeeId = employeeId,
            BasicSalary = basicSalary,
            Allowances = allowances,
            Deductions = deductions,
            IncomeTaxPph21 = incomeTaxPph21,
            NetSalary = netSalary,
            IsPaid = false
        });
    }

    public void Approve(Guid approvedByUserId)
    {
        Status = PayrollStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        UpdatedByUserId = approvedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkPaid()
    {
        Status = PayrollStatus.Paid;
        foreach (var item in Items)
        {
            item.IsPaid = true;
        }
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

public class PayrollItem
{
    public Guid Id { get; set; }
    public Guid PeriodId { get; set; }
    public Guid EmployeeId { get; set; }
    public decimal BasicSalary { get; set; }
    public decimal Allowances { get; set; }
    public decimal Deductions { get; set; }
    public decimal IncomeTaxPph21 { get; set; }
    public decimal NetSalary { get; set; }
    public bool IsPaid { get; set; }

    public PayrollPeriod Period { get; set; } = default!;
    public Employee Employee { get; set; } = default!;
}
