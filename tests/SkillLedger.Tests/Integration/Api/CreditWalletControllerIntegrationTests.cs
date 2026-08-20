using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Credit Wallet API endpoints
/// Tests wallet operations, transfers, escrow, and transaction history
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class CreditWalletControllerIntegrationTests : IntegrationTestBase
{
    private ICreditWalletService _walletService = null!;
    private User _user = null!;
    private User _otherUser = null!;
    private Project _project = null!;

    public CreditWalletControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        _walletService = ServiceScope.ServiceProvider.GetRequiredService<ICreditWalletService>();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "wallet-user@test.com",
            UserName = "wallet-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "wallet-other@test.com",
            UserName = "wallet-other@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);

        // Setup test project
        _project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _user.Id,
            Title = "Wallet Test Project",
            Description = "Project for wallet testing",
            CreditBudget = 500,
            Status = ProjectStatus.Published,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        Context.Projects.Add(_project);
        await Context.SaveChangesAsync();

        // Create wallets for users
        await _walletService.CreateWalletAsync(_user.Id);
        await _walletService.CreateWalletAsync(_otherUser.Id);
    }

    #region CSRF-Protected Request Helpers

    /// <summary>
    /// Sends a POST request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> PostWithCsrfAsync<T>(string url, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.PostAsync(url, content);
    }

    #endregion

    #region GET /api/credit-wallet Tests

    [Fact]
    [FastTest]
    public async Task GET_Wallet_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("wallet", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_Wallet_ReturnsWalletDetails()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Test credits", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();

        var wallet = result.GetProperty("wallet");
        // Wallet starts with 100 credits + 1000 added = 1100 total
        wallet.GetProperty("currentBalance").GetInt32().Should().Be(1100);
    }

    [Fact]
    [FastTest]
    public async Task GET_Wallet_IncludesRecentTransactions()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 500, "Transaction 1", CreditTransactionType.Purchase);
        await _walletService.AddCreditsAsync(_user.Id, 300, "Transaction 2", CreditTransactionType.Purchase);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("recentTransactions", out var transactions).Should().BeTrue();
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [FastTest]
    public async Task GET_Wallet_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/credit-wallet");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/credit-wallet/balance Tests

    [Fact]
    [FastTest]
    public async Task GET_Balance_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [FastTest]
    public async Task GET_Balance_ReturnsCurrentBalance()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 750, "Test credits", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        // Wallet starts with 100 credits + 750 added = 850 total
        result.GetProperty("balance").GetInt32().Should().Be(850);
    }

    [Fact]
    [FastTest]
    public async Task GET_Balance_IncludesAvailableBalance()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Test credits", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("availableBalance", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_Balance_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/credit-wallet/balance");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/credit-wallet/add-credits Tests

    [Fact]
    [FastTest]
    public async Task POST_AddCredits_WithValidAmount_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user, new[] { "Admin" });

        var request = new
        {
            Amount = 500,
            Description = "Test purchase"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/add-credits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        // New wallets start with 100 credits, so adding 500 results in 600 total
        result.GetProperty("newBalance").GetInt32().Should().Be(600);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AddCredits_AsNonAdmin_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Amount = 500,
            Description = "Unauthorized mint attempt"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/add-credits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddCredits_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user, new[] { "Admin" });

        var request = new
        {
            Amount = 0,
            Description = "Zero credits"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/add-credits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddCredits_WithNegativeAmount_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user, new[] { "Admin" });

        var request = new
        {
            Amount = -100,
            Description = "Negative credits"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/add-credits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_AddCredits_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Amount = 500,
            Description = "Test purchase"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/add-credits", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/credit-wallet/transfer Tests

    [Fact]
    [FastTest]
    public async Task POST_Transfer_WithValidData_ReturnsOk()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        var request = new
        {
            ToUserId = _otherUser.Id,
            Amount = 200,
            Description = "Test transfer",
            TransactionType = "DirectPayment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        // Wallet starts with 100, adds 1000 (=1100), transfers 200 = 900 remaining
        result.GetProperty("newBalance").GetInt32().Should().Be(900);
    }

    [Fact]
    [FastTest]
    public async Task POST_Transfer_InsufficientBalance_ReturnsBadRequest()
    {
        // Arrange - User has 0 balance
        AuthenticateAs(_user);

        var request = new
        {
            ToUserId = _otherUser.Id,
            Amount = 500,
            Description = "Transfer without funds"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Transfer_ToSelf_ReturnsBadRequest()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        var request = new
        {
            ToUserId = _user.Id, // Same user - self transfer
            Amount = 100,
            Description = "Self transfer attempt"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("message").GetString().Should().Contain("yourself");
    }

    [Fact]
    [FastTest]
    public async Task POST_Transfer_ToNonExistentUser_ReturnsBadRequest()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        var request = new
        {
            ToUserId = Guid.NewGuid(), // Non-existent user
            Amount = 100,
            Description = "Transfer to non-existent user"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Transfer_WithZeroAmount_ReturnsBadRequest()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        var request = new
        {
            ToUserId = _otherUser.Id,
            Amount = 0,
            Description = "Zero transfer",
            TransactionType = "DirectPayment"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Transfer_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ToUserId = _otherUser.Id,
            Amount = 100,
            Description = "Unauthorized transfer"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/transfer", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/credit-wallet/escrow Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithValidData_ReturnsOk()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        var request = new
        {
            ProjectId = _project.Id,
            Amount = 500
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/escrow", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_InsufficientBalance_ReturnsBadRequest()
    {
        // Arrange - User has 0 balance
        AuthenticateAs(_user);

        var request = new
        {
            ProjectId = _project.Id,
            Amount = 500
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/escrow", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateEscrow_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            ProjectId = _project.Id,
            Amount = 500
        };

        // Act
        var response = await PostWithCsrfAsync("/api/credit-wallet/escrow", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/credit-wallet/transactions Tests

    [Fact]
    [FastTest]
    public async Task GET_Transactions_WithAuthenticatedUser_ReturnsOk()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 500, "Test transaction", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    [FastTest]
    public async Task GET_Transactions_ReturnsTransactionList()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 500, "Transaction 1", CreditTransactionType.Purchase);
        await _walletService.AddCreditsAsync(_user.Id, 300, "Transaction 2", CreditTransactionType.Purchase);
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.TryGetProperty("transactions", out var transactions).Should().BeTrue();
        transactions.GetArrayLength().Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    [FastTest]
    public async Task GET_Transactions_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        for (int i = 0; i < 25; i++)
        {
            await _walletService.AddCreditsAsync(_user.Id, 10, $"Transaction {i}", CreditTransactionType.StartingCredit);
        }

        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/credit-wallet/transactions?page=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("transactions").GetArrayLength().Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    [FastTest]
    public async Task GET_Transactions_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/credit-wallet/transactions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication
        var endpoints = new[]
        {
            ("GET", "/api/credit-wallet"),
            ("GET", "/api/credit-wallet/balance"),
            ("GET", "/api/credit-wallet/transactions")
        };

        foreach (var (method, url) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(url);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {url} should require authentication");
        }
    }

    #endregion

    #region Balance Consistency Tests

    [Fact]
    [FastTest]
    public async Task Wallet_BalanceConsistency_AfterMultipleOperations()
    {
        // Arrange
        await _walletService.AddCreditsAsync(_user.Id, 1000, "Starting balance", CreditTransactionType.StartingCredit);
        AuthenticateAs(_user);

        // Act - Multiple operations
        var transfer = new
        {
            ToUserId = _otherUser.Id,
            Amount = 200,
            Description = "Transfer",
            TransactionType = "DirectPayment"
        };
        var transferResponse = await PostWithCsrfAsync("/api/credit-wallet/transfer", transfer);
        transferResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        AuthenticateAs(_user, new[] { "Admin" });
        var addCredits = new
        {
            Amount = 300,
            Description = "Add more"
        };
        var addCreditsResponse = await PostWithCsrfAsync("/api/credit-wallet/add-credits", addCredits);
        addCreditsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get final balance
        var balanceResponse = await Client.GetAsync("/api/credit-wallet/balance");
        var result = await balanceResponse.Content.ReadFromJsonAsync<JsonElement>();

        // Assert - Starts with 100, adds 1000 (=1100), transfers 200 (=900), adds 300 = 1200
        result.GetProperty("balance").GetInt32().Should().Be(1200);
    }

    #endregion
}
