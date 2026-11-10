using ERP.Domain.Common;
using ERP.Domain.Entities.Master;

namespace ERP.Domain.Entities.Hr;

public enum EmploymentStatus
{
    Permanent,
    Contract,
    Probation
}

public class Employee : BaseEntity<Guid>
{
    public Guid DepartmentId { get; private set; }
    public Guid? UserId { get; private set; }
    public string EmployeeNumber { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string IdentityCardNumber { get; private set; } = string.Empty; // NIK/KTP
    public DateOnly BirthDate { get; private set; }
    public DateOnly HireDate { get; private set; }
    public EmploymentStatus EmploymentStatus { get; private set; } = EmploymentStatus.Permanent;
    public decimal BasicSalary { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Company Company { get; private set; } = default!;
    public Branch Branch { get; private set; } = default!;
    public Department Department { get; private set; } = default!;

    private Employee() { }

    public static Employee Create(
        Guid companyId,
        Guid branchId,
        Guid departmentId,
        string employeeNumber,
        string fullName,
        string identityCardNumber,
        DateOnly birthDate,
        DateOnly hireDate,
        EmploymentStatus employmentStatus,
        decimal basicSalary,
        Guid createdByUserId,
        Guid? userId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(identityCardNumber);
        if (basicSalary < 0) throw new ArgumentOutOfRangeException(nameof(basicSalary), "Gaji pokok tidak boleh negatif.");

        return new Employee
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            DepartmentId = departmentId,
            UserId = userId,
            EmployeeNumber = employeeNumber.Trim().ToUpperInvariant(),
            FullName = fullName.Trim(),
            IdentityCardNumber = identityCardNumber.Trim(),
            BirthDate = birthDate,
            HireDate = hireDate,
            EmploymentStatus = employmentStatus,
            BasicSalary = basicSalary,
            IsActive = true,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = DateTime.UtcNow,
            IsDeleted = false
        };
    }

    public void Update(
        Guid departmentId,
        string fullName,
        DateOnly birthDate,
        EmploymentStatus employmentStatus,
        decimal basicSalary,
        bool isActive,
        Guid updatedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        if (basicSalary < 0) throw new ArgumentOutOfRangeException(nameof(basicSalary), "Gaji pokok tidak boleh negatif.");

        DepartmentId = departmentId;
        FullName = fullName.Trim();
        BirthDate = birthDate;
        EmploymentStatus = employmentStatus;
        BasicSalary = basicSalary;
        IsActive = isActive;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

