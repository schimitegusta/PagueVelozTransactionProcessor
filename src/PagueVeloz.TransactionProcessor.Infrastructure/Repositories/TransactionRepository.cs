using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly ApplicationDbContext _context;

        public TransactionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.TransactionSet
                .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        }

        public async Task<Transaction?> GetByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
        {
            return await _context.TransactionSet
                .FirstOrDefaultAsync(t => t.ReferenceId == referenceId, cancellationToken);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(
            Guid accountId,
            int? limit = null,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Transaction> query = _context.TransactionSet
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.ProcessedAt);

            if (limit.HasValue)
                query = query.Take(limit.Value);

            return await query.ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Transaction transaction, CancellationToken cancellationToken = default)
        {
            await _context.TransactionSet.AddAsync(transaction, cancellationToken);
        }

        public async Task<bool> ExistsByReferenceIdAsync(string referenceId, CancellationToken cancellationToken = default)
        {
            return await _context.TransactionSet
                .AnyAsync(t => t.ReferenceId == referenceId, cancellationToken);
        }
    }
}
