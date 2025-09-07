using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using PagueVeloz.TransactionProcessor.Application.DTOs;
using PagueVeloz.TransactionProcessor.Domain.Entities;
using PagueVeloz.TransactionProcessor.Domain.Interfaces;
using PagueVeloz.TransactionProcessor.Infrastructure.Data;
using System.Net;
using System.Net.Http.Json;

namespace PagueVeloz.TransactionProcessor.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(descriptor =>
                    descriptor.ServiceType.Namespace?.Contains("Microsoft.EntityFrameworkCore") == true ||
                    descriptor.ServiceType == typeof(ApplicationDbContext) ||
                    (descriptor.ServiceType.IsGenericType &&
                     descriptor.ServiceType.GetGenericTypeDefinition() == typeof(DbContextOptions<>)) ||
                    descriptor.ServiceType == typeof(DbContextOptions) ||
                    descriptor.ImplementationType?.Namespace?.Contains("Microsoft.EntityFrameworkCore") == true
                ).ToArray();

                foreach (var descriptor in toRemove)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase($"TestDatabase")
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                    .EnableSensitiveDataLogging()
                    .EnableDetailedErrors();
                });
            });

            builder.UseEnvironment("Testing");
        }
    }

    public class TransactionApiTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public TransactionApiTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            context.Database.EnsureCreated();
            SeedTestData(context);
        }

        private static void SeedTestData(ApplicationDbContext context)
        {
            if (context.ClientSet.Any())
            {
                context.ClientSet.RemoveRange(context.ClientSet);
                context.SaveChanges();
            }

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
                Amount = 10000,
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
                InitialBalance = 100000,
                CreditLimit = 50000,
                Currency = "BRL"
            };

            var response = await _client.PostAsJsonAsync("/api/accounts", createAccountRequest);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Criação de conta falhou: {response.StatusCode} - {errorContent}");
            }

            response.EnsureSuccessStatusCode();
            var account = await response.Content.ReadFromJsonAsync<AccountResponse>();
            return Guid.Parse(account!.AccountId);
        }

        public void Dispose()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
    }
}