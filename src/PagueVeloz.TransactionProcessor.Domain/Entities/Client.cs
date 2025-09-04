using PagueVeloz.TransactionProcessor.Domain.Events;

namespace PagueVeloz.TransactionProcessor.Domain.Entities
{
    public class Client : BaseEntity
    {
        public string Name { get; private set; }
        public string Document { get; private set; }
        public string Email { get; private set; }
        public bool IsActive { get; private set; }

        private readonly List<Account> _accounts = new();
        public IReadOnlyCollection<Account> Accounts => _accounts.AsReadOnly();

        protected Client() { }

        public Client(string name, string document, string email)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            IsActive = true;
        }

        public Account CreateAccount(decimal initialBalance, decimal creditLimit, string currency)
        {
            var account = new Account(Id, initialBalance, creditLimit, currency);
            _accounts.Add(account);

            AddDomainEvent(new AccountCreatedEvent(account.Id, Id, initialBalance, creditLimit));

            return account;
        }

        public void Activate()
        {
            IsActive = true;
            SetUpdatedAt();
        }

        public void Deactivate()
        {
            IsActive = false;
            SetUpdatedAt();
        }
    }
}
