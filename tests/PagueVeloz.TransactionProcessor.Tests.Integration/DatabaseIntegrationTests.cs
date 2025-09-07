using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using PagueVeloz.TransactionProcessor.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace PagueVeloz.TransactionProcessor.Tests.Integration
{
    public class DatabaseIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AccountRepository _accountRepository;
        private readonly TransactionRepository _transactionRepository;
        private static readonly InMemoryDatabaseRoot _databaseRoot = new InMemoryDatabaseRoot();

        public DatabaseIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _accountRepository = new AccountRepository(_context);
            _transactionRepository = new TransactionRepository(_context);
        }

        [Fact]
        public async Task Should_Save_And_Retrieve_Account()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 1000, 500, "BRL");

            // Act
            await _accountRepository.AddAsync(account);
            await _context.SaveChangesAsync();

            var retrievedAccount = await _accountRepository.GetByIdAsync(account.Id);

            // Assert
            retrievedAccount.Should().NotBeNull();
            retrievedAccount!.Id.Should().Be(account.Id);
            retrievedAccount.Balance.Should().Be(1000);
            retrievedAccount.CreditLimit.Should().Be(500);
        }

        [Fact]
        public async Task Should_Handle_Concurrent_Updates_With_RowVersion()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=TestDb;Trusted_Connection=True;")
                .Options;

            using var _context = new ApplicationDbContext(options);
            await _context.Database.EnsureCreatedAsync();

            string randomStrDoc = Guid.NewGuid().ToString("N").Substring(0, 19);
            var client = new Client("name", randomStrDoc, "email");
            await _context.ClientSet.AddAsync(client);
            await _context.SaveChangesAsync();

            var account = new Account(client.Id, 1000, 0, "BRL");
            await _context.AccountSet.AddAsync(account);
            await _context.SaveChangesAsync();

            using var context1 = new ApplicationDbContext(options);
            using var context2 = new ApplicationDbContext(options);

            var account1 = await context1.AccountSet.FirstAsync(a => a.Id == account.Id);
            var account2 = await context2.AccountSet.FirstAsync(a => a.Id == account.Id);

            // Act
            string randomStrRefId1 = Guid.NewGuid().ToString("N").Substring(0, 6);
            account1.Credit(new(100, "BRL"), randomStrRefId1);
            await context1.SaveChangesAsync();

            string randomStrRefId2 = Guid.NewGuid().ToString("N").Substring(0, 6);
            account2.Credit(new(200, "BRL"), randomStrRefId2);

            // Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            {
                await context2.SaveChangesAsync();
            });
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
