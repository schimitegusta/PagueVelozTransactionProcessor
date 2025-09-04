using PagueVeloz.TransactionProcessor.Domain.Entities;

namespace PagueVeloz.TransactionProcessor.Domain.Interfaces
{
    public interface IClientRepository
    {
        Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Client?> GetByIdWithAccountsAsync(Guid id, CancellationToken cancellationToken = default);
        Task<Client?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);
        Task AddAsync(Client client, CancellationToken cancellationToken = default);
        Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    }
}