using PagueVeloz.TransactionProcessor.Domain.Events;
using System.ComponentModel.DataAnnotations.Schema;

namespace PagueVeloz.TransactionProcessor.Domain.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; }
        public DateTime CreatedAt { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [NotMapped]
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected BaseEntity()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.UtcNow;
        }

        protected BaseEntity(Guid id)
        {
            Id = id;
            CreatedAt = DateTime.UtcNow;
        }

        public void AddDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Add(domainEvent);
        }

        public void RemoveDomainEvent(IDomainEvent domainEvent)
        {
            _domainEvents.Remove(domainEvent);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }

        protected void SetUpdatedAt()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
