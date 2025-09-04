using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Domain.Interfaces
{
    public interface ITransactionRepository
    {
        Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(Guid accountId, int? limit = null, CancellationToken cancellationToken = default);
        Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default);
        Task<bool> ExistsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default);
    }
}