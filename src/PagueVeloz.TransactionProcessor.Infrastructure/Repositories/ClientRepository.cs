using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Repositories
{
    public class ClientRepository : IClientRepository
    {
        private readonly ApplicationDbContext _context;

        public ClientRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ClientSet
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Client?> GetByIdWithAccountsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ClientSet
                .Include(c => c.Accounts)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        }

        public async Task<Client?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default)
        {
            return await _context.ClientSet
                .FirstOrDefaultAsync(c => c.Document == document, cancellationToken);
        }

        public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
        {
            await _context.ClientSet.AddAsync(client, cancellationToken);
        }

        public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
        {
            _context.ClientSet.Update(client);
            return Task.CompletedTask;
        }

        public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _context.ClientSet
                .AnyAsync(c => c.Id == id, cancellationToken);
        }
    }
}
