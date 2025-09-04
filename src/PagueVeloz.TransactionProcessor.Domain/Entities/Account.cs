using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.ValueObjects;
using PagueVeloz.TransactionProcessor.Domain.Events;

namespace PagueVeloz.TransactionProcessor.Domain.Entities
{
    public class Account : BaseEntity
    {
        public Guid ClientId { get; private set; }
        public decimal Balance { get; private set; }
        public decimal ReservedBalance { get; private set; }
        public decimal CreditLimit { get; private set; }
        public string Currency { get; private set; }
        public AccountStatus Status { get; private set; }

        private readonly List<Transaction> _transactions = new();
        public IReadOnlyCollection<Transaction> Transactions => _transactions.AsReadOnly();

        public decimal AvailableBalance => Balance - ReservedBalance;
        public decimal TotalAvailable => AvailableBalance + CreditLimit;

        protected Account() { }

        public Account(Guid clientId, decimal initialBalance, decimal creditLimit, string currency)
        {
            ClientId = clientId;
            Balance = initialBalance;
            ReservedBalance = 0;
            CreditLimit = creditLimit;
            Currency = currency ?? throw new ArgumentNullException(nameof(currency));
            Status = AccountStatus.Active;
        }

        public Transaction Credit(Money amount, string referenceId, Dictionary<string, object>? metadata = null)
        {
            ValidateActive();
            ValidateCurrency(amount.Currency);

            Balance += amount.Amount;
            SetUpdatedAt();

            var transaction = new Transaction(
                Id,
                OperationType.Credit,
                amount.Amount,
                amount.Currency,
                referenceId,
                TransactionStatus.Success,
                metadata,
                Balance,
                ReservedBalance,
                AvailableBalance
            );

            _transactions.Add(transaction);
            AddDomainEvent(new TransactionProcessedEvent(transaction));

            return transaction;
        }

        public Transaction Debit(Money amount, string referenceId, Dictionary<string, object>? metadata = null)
        {
            ValidateActive();
            ValidateCurrency(amount.Currency);

            if (TotalAvailable < amount.Amount)
            {
                var failedTransaction = new Transaction(
                    Id,
                    OperationType.Debit,
                    amount.Amount,
                    amount.Currency,
                    referenceId,
                    TransactionStatus.Failed,
                    metadata,
                    Balance,
                    ReservedBalance,
                    AvailableBalance,
                    "Fundos insuficientes"
                );

                _transactions.Add(failedTransaction);
                return failedTransaction;
            }

            Balance -= amount.Amount;
            SetUpdatedAt();

            var transaction = new Transaction(
                Id,
                OperationType.Debit,
                amount.Amount,
                amount.Currency,
                referenceId,
                TransactionStatus.Success,
                metadata,
                Balance,
                ReservedBalance,
                AvailableBalance
            );

            _transactions.Add(transaction);
            AddDomainEvent(new TransactionProcessedEvent(transaction));

            return transaction;
        }

        public Transaction Reserve(Money amount, string referenceId, Dictionary<string, object>? metadata = null)
        {
            ValidateActive();
            ValidateCurrency(amount.Currency);

            if (AvailableBalance < amount.Amount)
            {
                var failedTransaction = new Transaction(
                    Id,
                    OperationType.Reserve,
                    amount.Amount,
                    amount.Currency,
                    referenceId,
                    TransactionStatus.Failed,
                    metadata,
                    Balance,
                    ReservedBalance,
                    AvailableBalance,
                    "Saldo disponível insuficiente"
                );

                _transactions.Add(failedTransaction);
                return failedTransaction;
            }

            ReservedBalance += amount.Amount;
            SetUpdatedAt();

            var transaction = new Transaction(
                Id,
                OperationType.Reserve,
                amount.Amount,
                amount.Currency,
                referenceId,
                TransactionStatus.Success,
                metadata,
                Balance,
                ReservedBalance,
                AvailableBalance
            );

            _transactions.Add(transaction);
            AddDomainEvent(new TransactionProcessedEvent(transaction));

            return transaction;
        }

        public Transaction Capture(Money amount, string referenceId, Dictionary<string, object>? metadata = null)
        {
            ValidateActive();
            ValidateCurrency(amount.Currency);

            if (ReservedBalance < amount.Amount)
            {
                var failedTransaction = new Transaction(
                    Id,
                    OperationType.Capture,
                    amount.Amount,
                    amount.Currency,
                    referenceId,
                    TransactionStatus.Failed,
                    metadata,
                    Balance,
                    ReservedBalance,
                    AvailableBalance,
                    "Saldo reservado insuficiente"
                );

                _transactions.Add(failedTransaction);
                return failedTransaction;
            }

            ReservedBalance -= amount.Amount;
            Balance -= amount.Amount;
            SetUpdatedAt();

            var transaction = new Transaction(
                Id,
                OperationType.Capture,
                amount.Amount,
                amount.Currency,
                referenceId,
                TransactionStatus.Success,
                metadata,
                Balance,
                ReservedBalance,
                AvailableBalance
            );

            _transactions.Add(transaction);
            AddDomainEvent(new TransactionProcessedEvent(transaction));

            return transaction;
        }

        public void Block()
        {
            Status = AccountStatus.Blocked;
            SetUpdatedAt();
            AddDomainEvent(new AccountBlockedEvent(Id));
        }

        public void Unblock()
        {
            Status = AccountStatus.Active;
            SetUpdatedAt();
            AddDomainEvent(new AccountUnblockedEvent(Id));
        }

        private void ValidateActive()
        {
            if (Status != AccountStatus.Active)
                throw new InvalidOperationException($"A conta {Id} não está ativa");
        }

        private void ValidateCurrency(string currency)
        {
            if (Currency != currency)
                throw new InvalidOperationException($"Incompatibilidade de moedas. Moeda da conta: {Currency}, Moeda de transação: {currency}");
        }
    }
}
