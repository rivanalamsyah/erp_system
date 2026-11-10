using ERP.Domain.Entities.Finance;
using FluentAssertions;
using Xunit;

namespace ERP.Domain.Tests;

public class FinanceTests
{
    [Fact]
    public void JournalEntry_Post_WhenBalanced_Succeeds()
    {
        // Arrange
        var entry = JournalEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "JV-2026-0001",
            DateOnly.FromDateTime(DateTime.Today),
            "Manual",
            "MEMO-001",
            "Pencatatan Biaya Listrik",
            Guid.NewGuid());

        var bebanListrikId = Guid.NewGuid();
        var kasId = Guid.NewGuid();

        // Act: Debit 500,000 & Credit 500,000
        entry.AddDebitLine(bebanListrikId, 500_000, "Beban listrik kantor");
        entry.AddCreditLine(kasId, 500_000, "Pengeluaran kas kecil");

        var approverId = Guid.NewGuid();
        entry.Post(approverId);

        // Assert
        entry.Status.Should().Be(JournalStatus.Posted);
        entry.TotalDebit.Should().Be(500_000);
        entry.TotalCredit.Should().Be(500_000);
        entry.PostedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void JournalEntry_Post_WhenUnbalanced_ThrowsBR_ACC_001_Exception()
    {
        // Arrange
        var entry = JournalEntry.Create(
            Guid.NewGuid(), Guid.NewGuid(),
            "JV-2026-0002",
            DateOnly.FromDateTime(DateTime.Today),
            "Manual",
            "MEMO-002",
            "Jurnal Tidak Seimbang",
            Guid.NewGuid());

        // Act: Debit 500,000 & Credit 400,000 -> Selisih 100,000
        entry.AddDebitLine(Guid.NewGuid(), 500_000);
        entry.AddCreditLine(Guid.NewGuid(), 400_000);

        // Assert
        var act = () => entry.Post(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*BR-ACC-001*");
    }

    [Fact]
    public void Payment_Post_WhenAllocationsExceedTotal_ThrowsException()
    {
        // Arrange
        var payment = Payment.Create(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "PV-2026-0001",
            DateOnly.FromDateTime(DateTime.Today),
            PaymentType.VendorPayment,
            Guid.NewGuid(),
            PaymentMethod.BankTransfer,
            1_000_000,
            Guid.NewGuid());

        // Act: Allocate 1,200,000 on a 1,000,000 payment
        payment.AddAllocation(Guid.NewGuid(), 1_200_000);

        // Assert
        var act = () => payment.Post(Guid.NewGuid());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*tidak boleh melebihi*");
    }
}

