using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.ValueObjects;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using FluentAssertions;

namespace PagueVeloz.TransactionProcessor.Tests.Unit.Domain
{
    public class AccountTests
    {
        [Fact]
        public void Credit_Should_Increase_Balance()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 0, 1000, "BRL");
            var amount = new Money(500, "BRL");

            // Act
            var transaction = account.Credit(amount, "REF-001");

            // Assert
            account.Balance.Should().Be(500);
            account.AvailableBalance.Should().Be(500);
            transaction.Status.Should().Be(TransactionStatus.Success);
            transaction.BalanceAfter.Should().Be(500);
        }

        [Fact]
        public void Debit_Should_Fail_When_Insufficient_Funds()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 100, 0, "BRL");
            var amount = new Money(200, "BRL");

            // Act
            var transaction = account.Debit(amount, "REF-002");

            // Assert
            account.Balance.Should().Be(100);
            transaction.Status.Should().Be(TransactionStatus.Failed);
            transaction.ErrorMessage.Should().Be("Exedeu limite de crédito");
        }

        [Fact]
        public void Debit_Should_Use_Credit_Limit()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 100, 200, "BRL");
            var amount = new Money(250, "BRL");

            // Act
            var transaction = account.Debit(amount, "REF-003");

            // Assert
            account.Balance.Should().Be(-150);
            transaction.Status.Should().Be(TransactionStatus.Success);
            transaction.BalanceAfter.Should().Be(-150);
        }

        [Fact]
        public void Reserve_Should_Block_Available_Balance()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 1000, 0, "BRL");
            var amount = new Money(300, "BRL");

            // Act
            var transaction = account.Reserve(amount, "REF-004");

            // Assert
            account.Balance.Should().Be(1000);
            account.ReservedBalance.Should().Be(300);
            account.AvailableBalance.Should().Be(700);
            transaction.Status.Should().Be(TransactionStatus.Success);
        }

        [Fact]
        public void Capture_Should_Debit_From_Reserved_Balance()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 1000, 0, "BRL");
            var reserveAmount = new Money(300, "BRL");
            var captureAmount = new Money(300, "BRL");

            // Act
            account.Reserve(reserveAmount, "REF-005");
            var captureTransaction = account.Capture(captureAmount, "REF-006");

            // Assert
            account.Balance.Should().Be(700);
            account.ReservedBalance.Should().Be(0);
            account.AvailableBalance.Should().Be(700);
            captureTransaction.Status.Should().Be(TransactionStatus.Success);
        }

        [Fact]
        public void Should_Throw_When_Currency_Mismatch()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 1000, 0, "BRL");
            var amount = new Money(100, "USD");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => account.Credit(amount, "REF-007"));
        }

        [Fact]
        public void Should_Not_Allow_Operations_When_Blocked()
        {
            // Arrange
            var account = new Account(Guid.NewGuid(), 1000, 0, "BRL");
            account.Block();
            var amount = new Money(100, "BRL");

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => account.Credit(amount, "REF-008"));
        }
    }
}
