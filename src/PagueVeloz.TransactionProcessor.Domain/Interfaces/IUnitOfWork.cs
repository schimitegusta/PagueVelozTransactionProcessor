namespace PagueVeloz.TransactionProcessor.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        ITransactionRepository Transactions { get; }
        IClientRepository Clients { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }
}
