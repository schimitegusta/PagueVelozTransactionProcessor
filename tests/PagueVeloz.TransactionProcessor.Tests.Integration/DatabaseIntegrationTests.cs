using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using PagueVeloz.TransactionProcessor.Infrastructure.Repositories;

namespace PagueVeloz.TransactionProcessor.Tests.Integration
{
    public class DatabaseIntegrationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AccountRepository _accountRepository;
        private readonly TransactionRepository _transactionRepository;

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
            var account = new Account(Guid.NewGuid(), 1000, 0, "BRL");
            await _accountRepository.AddAsync(account);
            await _context.SaveChangesAsync();

            // Simular duas atualizações concorrentes
            var options1 = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Concurrent1")
                .Options;
            var context1 = new ApplicationDbContext(options1);

            var options2 = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb_Concurrent2")
                .Options;
            var context2 = new ApplicationDbContext(options2);

            var account1 = await context1.AccountSet.FirstAsync(a => a.Id == account.Id);
            var account2 = await context2.AccountSet.FirstAsync(a => a.Id == account.Id);

            // Act
            account1.Credit(new(100, "BRL"), "REF-001");
            await context1.SaveChangesAsync();

            account2.Credit(new(200, "BRL"), "REF-002");

            // Assert
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
            {
                await context2.SaveChangesAsync();
            });

            // Cleanup
            context1.Dispose();
            context2.Dispose();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
