namespace Catalog.Core.Common;

public class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public override string ToString() => $"{Amount:F2} {Currency}";

    public override bool Equals(object? obj)
    {
        if (obj is Money other)
            return Amount == other.Amount && Currency == other.Currency;
        return false;
    }

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);
}