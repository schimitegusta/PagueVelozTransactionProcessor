using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Domain.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
        Task<IEnumerable<Account>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default);
        Task AddAsync(Account account, CancellationToken cancellationToken = default);
        Task UpdateAsync(Account account, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
