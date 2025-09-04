namespace PagueVeloz.TransactionProcessor.Domain.ValueObjects
{
    public class Money : IEquatable<Money>
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            if (amount < 0)
                throw new ArgumentException("O valor não pode ser negativo", nameof(amount));

            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("A moeda não pode estar vazia", nameof(currency));

            Amount = amount;
            Currency = currency.ToUpperInvariant();
        }

        public static Money Zero(string currency) => new(0, currency);

        public Money Add(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Não é possível adicionar dinheiro com moedas diferentes: {Currency} e {other.Currency}");

            return new Money(Amount + other.Amount, Currency);
        }

        public Money Subtract(Money other)
        {
            if (Currency != other.Currency)
                throw new InvalidOperationException($"Não é possível subtrair dinheiro com moedas diferentes: {Currency} e {other.Currency}");

            return new Money(Amount - other.Amount, Currency);
        }

        public bool CanSubtract(Money other)
        {
            return Currency == other.Currency && Amount >= other.Amount;
        }

        public bool Equals(Money? other)
        {
            if (other is null) return false;
            return Amount == other.Amount && Currency == other.Currency;
        }

        public override bool Equals(object? obj) => Equals(obj as Money);
        public override int GetHashCode() => HashCode.Combine(Amount, Currency);
        public override string ToString() => $"{Currency} {Amount:F2}";
    }
}
