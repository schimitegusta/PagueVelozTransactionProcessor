using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PagueVeloz.TransactionProcessor.Application.Commands;
using PagueVeloz.TransactionProcessor.Application.Handlers;
using PagueVeloz.TransactionProcessor.Application.Services;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Enums;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;

namespace PagueVeloz.TransactionProcessor.Tests.Unit.Application
{
    public class ProcessTransactionHandlerTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<ILogger<ProcessTransactionHandler>> _loggerMock;
        private readonly ProcessTransactionHandler _handler;

        public ProcessTransactionHandlerTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _loggerMock = new Mock<ILogger<ProcessTransactionHandler>>();

            _handler = new ProcessTransactionHandler(
                _unitOfWorkMock.Object,
                _eventPublisherMock.Object,
                _loggerMock.Object);
        }

        [Fact]
        public async Task Should_Process_Credit_Transaction_Successfully()
        {
            // Arrange
            var accountId = Guid.NewGuid();
            var account = new Account(Guid.NewGuid(), 0, 0, "BRL");

            var command = new ProcessTransactionCommand
            {
                Operation = OperationType.Credit,
                AccountId = accountId,
                Amount = 100,
                Currency = "BRL",
                ReferenceId = "REF-001"
            };

            _unitOfWorkMock.Setup(x => x.Transactions.GetByReferenceIdAsync(It.IsAny<string>(), default))
                .ReturnsAsync((Transaction?)null);

            _unitOfWorkMock.Setup(x => x.Accounts.GetByIdForUpdateAsync(accountId, default))
                .ReturnsAsync(account);

            _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(default))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(x => x.CommitTransactionAsync(default))
                .Returns(Task.CompletedTask);

            _unitOfWorkMock.Setup(x => x.SaveEntitiesAsync(default))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Status.Should().Be("success");
            result.Balance.Should().Be(10000);

            _unitOfWorkMock.Verify(x => x.Accounts.UpdateAsync(It.IsAny<Account>(), default), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveEntitiesAsync(default), Times.Once);
        }

        [Fact]
        public async Task Should_Return_Existing_Transaction_For_Duplicate_ReferenceId()
        {
            // Arrange
            var existingTransaction = new Transaction(
                Guid.NewGuid(),
                OperationType.Credit,
                100,
                "BRL",
                "REF-001",
                TransactionStatus.Success,
                null,
                100,
                0,
                100);

            var command = new ProcessTransactionCommand
            {
                Operation = OperationType.Credit,
                AccountId = Guid.NewGuid(),
                Amount = 100,
                Currency = "BRL",
                ReferenceId = "REF-001"
            };

            _unitOfWorkMock.Setup(x => x.Transactions.GetByReferenceIdAsync("REF-001", default))
                .ReturnsAsync(existingTransaction);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Status.Should().Be("success");
            result.TransactionId.Should().Be(existingTransaction.Id.ToString());

            _unitOfWorkMock.Verify(x => x.Accounts.GetByIdForUpdateAsync(It.IsAny<Guid>(), default), Times.Never);
        }
    }
}
