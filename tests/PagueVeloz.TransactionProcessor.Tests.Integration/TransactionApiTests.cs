using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;

namespace PagueVeloz.TransactionProcessor.Tests.Integration
{
    public class TransactionApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public TransactionApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remover o DbContext de produção
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Adicionar DbContext em memória para testes
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });

                    // Garantir que o banco seja criado
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    dbContext.Database.EnsureCreated();
                    SeedTestData(dbContext);
                });
            });

            _client = _factory.CreateClient();
        }

        private void SeedTestData(ApplicationDbContext context)
        {
            var client = new Client("Test Client", "12345678900", "test@test.com");
            var account = client.CreateAccount(1000, 500, "BRL");

            context.ClientSet.Add(client);
            context.SaveChanges();
        }

        [Fact]
        public async Task POST_Transaction_Should_Return_Success_For_Credit()
        {
            // Arrange
            var accountId = await CreateTestAccount();

            var request = new TransactionRequest
            {
                Operation = "credit",
                AccountId = accountId.ToString(),
                Amount = 10000, // R$ 100,00
                Currency = "BRL",
                ReferenceId = $"TEST-{Guid.NewGuid()}"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/transactions", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<TransactionResponse>();
            content.Should().NotBeNull();
            content!.Status.Should().Be("success");
            content.Balance.Should().BeGreaterThanOrEqualTo(10000);
        }

        [Fact]
        public async Task POST_Transaction_Should_Return_BadRequest_For_Invalid_Operation()
        {
            // Arrange
            var request = new TransactionRequest
            {
                Operation = "invalid",
                AccountId = Guid.NewGuid().ToString(),
                Amount = 10000,
                Currency = "BRL",
                ReferenceId = $"TEST-{Guid.NewGuid()}"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/transactions", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task POST_Batch_Should_Process_Multiple_Transactions()
        {
            // Arrange
            var accountId = await CreateTestAccount();

            var requests = new List<TransactionRequest>
        {
            new()
            {
                Operation = "credit",
                AccountId = accountId.ToString(),
                Amount = 5000,
                Currency = "BRL",
                ReferenceId = $"BATCH-1-{Guid.NewGuid()}"
            },
            new()
            {
                Operation = "debit",
                AccountId = accountId.ToString(),
                Amount = 2000,
                Currency = "BRL",
                ReferenceId = $"BATCH-2-{Guid.NewGuid()}"
            }
        };

            // Act
            var response = await _client.PostAsJsonAsync("/api/transactions/batch", requests);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<List<TransactionResponse>>();
            content.Should().NotBeNull();
            content!.Should().HaveCount(2);
            content[0].Status.Should().Be("success");
            content[1].Status.Should().Be("success");
        }

        private async Task<Guid> CreateTestAccount()
        {
            var createAccountRequest = new CreateAccountRequest
            {
                ClientId = Guid.NewGuid().ToString(),
                InitialBalance = 100000, // R$ 1.000,00
                CreditLimit = 50000, // R$ 500,00
                Currency = "BRL"
            };

            var response = await _client.PostAsJsonAsync("/api/accounts", createAccountRequest);
            response.EnsureSuccessStatusCode();

            var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
            return Guid.Parse(account!.AccountId);
        }
    }
}
