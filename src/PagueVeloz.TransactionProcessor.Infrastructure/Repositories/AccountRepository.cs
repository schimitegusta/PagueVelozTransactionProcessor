using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Account?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AccountSet
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Account?> GetByIdWithTransactionsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AccountSet
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        }

        public async Task<Account?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
        {
            if (_context.Database.IsInMemory() || _context.Database.IsSqlite())
            {
                return await _context.AccountSet
                    .Include(a => a.Transactions)
                    .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
            }

            var sql = @"
            SELECT * FROM Accounts WITH (UPDLOCK, ROWLOCK) 
            WHERE Id = {0}";

            var account = await _context.AccountSet
                .FromSqlRaw(sql, id)
                .Include(a => a.Transactions)
                .FirstOrDefaultAsync(cancellationToken);

            return account;
        }

        public async Task<IEnumerable<Account>> GetByClientIdAsync(Guid clientId, CancellationToken cancellationToken = default)
        {
            return await _context.AccountSet
                .Where(a => a.ClientId == clientId)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(Account account, CancellationToken cancellationToken = default)
        {
            await _context.AccountSet.AddAsync(account, cancellationToken);
        }

        public Task UpdateAsync(Account account, CancellationToken cancellationToken = default)
        {
            _context.AccountSet.Update(account);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.AccountSet
                .AnyAsync(a => a.Id == id, cancellationToken);
        }
    }
}
