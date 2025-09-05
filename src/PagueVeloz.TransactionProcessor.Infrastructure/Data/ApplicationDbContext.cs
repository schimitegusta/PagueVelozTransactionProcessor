using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Data.Configurations;
using System.Data;
using PagueVeloz.TransactionProcessor.Infrastructure.Repositories;

namespace PagueVeloz.TransactionProcessor.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext, IUnitOfWork
    {
        private IDbContextTransaction? _currentTransaction;

        public DbSet<Account> AccountSet { get; set; }
        public DbSet<Transaction> TransactionSet { get; set; }
        public DbSet<Client> ClientSet { get; set; }

        private IAccountRepository? _accountRepository;
        private ITransactionRepository? _transactionRepository;
        private IClientRepository? _clientRepository;

        public IAccountRepository Accounts =>
            _accountRepository ??= new AccountRepository(this);

        public ITransactionRepository Transactions =>
            _transactionRepository ??= new TransactionRepository(this);

        public IClientRepository Clients =>
            _clientRepository ??= new ClientRepository(this);

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new AccountConfiguration());
            modelBuilder.ApplyConfiguration(new TransactionConfiguration());
            modelBuilder.ApplyConfiguration(new ClientConfiguration());

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                var properties = entityType.ClrType.GetProperties()
                    .Where(p => p.PropertyType == typeof(decimal) || p.PropertyType == typeof(decimal?));

                foreach (var property in properties)
                {
                    modelBuilder.Entity(entityType.Name).Property(property.Name)
                        .HasColumnType("decimal(18,2)");
                }
            }
        }

        public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await base.SaveChangesAsync(cancellationToken);
                return true;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new InvalidOperationException("Conflito de simultaneidade detectado", ex);
            }
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null) return;

            _currentTransaction = await Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await SaveChangesAsync(cancellationToken);
                await _currentTransaction?.CommitAsync(cancellationToken)!;
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _currentTransaction?.RollbackAsync(cancellationToken)!;
            }
            finally
            {
                _currentTransaction?.Dispose();
                _currentTransaction = null;
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            base.Dispose();
        }
    }
}