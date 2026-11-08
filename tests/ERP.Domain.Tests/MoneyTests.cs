using FluentAssertions;
using ERP.Domain.Common;
using ERP.Domain.Exceptions;
using Xunit;

namespace ERP.Domain.Tests;

public class MoneyTests
{
    [Fact]
    public void Money_Creation_ShouldSetAmountAndUppercaseCurrency()
    {
        var money = new Money(150000.50m, "idr");

        money.Amount.Should().Be(150000.50m);
        money.Currency.Should().Be("IDR");
    }

    [Fact]
    public void Addition_WithSameCurrency_ShouldAddAmountsCorrectly()
    {
        var a = new Money(1000m, "IDR");
        var b = new Money(2500m, "IDR");

        var sum = a + b;

        sum.Amount.Should().Be(3500m);
        sum.Currency.Should().Be("IDR");
    }

    [Fact]
    public void Addition_WithDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        var a = new Money(1000m, "IDR");
        var b = new Money(50m, "USD");

        Action act = () => { var _ = a + b; };

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*mata uang berbeda*");
    }

    [Fact]
    public void Subtraction_WithSameCurrency_ShouldSubtractAmountsCorrectly()
    {
        var a = new Money(500m, "IDR");
        var b = new Money(200m, "IDR");

        var diff = a - b;

        diff.Amount.Should().Be(300m);
        diff.Currency.Should().Be("IDR");
    }

    [Fact]
    public void Multiplication_ByScalar_ShouldMultiplyAmount()
    {
        var money = new Money(100.50m, "IDR");

        var multiplied = money * 3m;

        multiplied.Amount.Should().Be(301.50m);
    }
}

