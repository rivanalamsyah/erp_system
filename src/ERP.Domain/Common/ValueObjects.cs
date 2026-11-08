namespace ERP.Domain.Common;

public readonly record struct Money
{
    public decimal Amount { get; init; }
    public string Currency { get; init; }

    public Money(decimal amount, string currency = "IDR")
    {
        Amount = amount;
        Currency = string.IsNullOrWhiteSpace(currency) ? "IDR" : currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "IDR") => new(0m, currency);

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Tidak dapat menjumlahkan mata uang berbeda: {a.Currency} vs {b.Currency}");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new InvalidOperationException($"Tidak dapat mengurangkan mata uang berbeda: {a.Currency} vs {b.Currency}");
        return new Money(a.Amount - b.Amount, a.Currency);
    }

    public static Money operator *(Money a, decimal multiplier) => new(a.Amount * multiplier, a.Currency);
    public static Money operator *(decimal multiplier, Money a) => new(a.Amount * multiplier, a.Currency);

    public override string ToString() => $"{Currency} {Amount:N2}";
}

public readonly record struct DocumentNumber(string Value)
{
    public static implicit operator string(DocumentNumber doc) => doc.Value;
    public static implicit operator DocumentNumber(string value) => new(value);
    public override string ToString() => Value;
}

