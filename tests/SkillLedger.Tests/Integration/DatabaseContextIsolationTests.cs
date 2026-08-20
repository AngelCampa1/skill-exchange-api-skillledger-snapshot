using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// Integration tests to validate database context isolation between test setup and HTTP requests
/// CRITICAL TEST: Ensures the fix for database context sharing works correctly
/// </summary>
[Collection("Integration Other")]
public class DatabaseContextIsolationTests : IntegrationTestBase
{
    private const string TestPassword = "DatabaseTestP@ss!w0rd123";

    public DatabaseContextIsolationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    /// <summary>
    /// Generate unique test email for each test to avoid conflicts
    /// TEST-CRIT-001 FIX: Use only Guid.NewGuid() instead of static counter to prevent parallel test conflicts
    /// </summary>
    private string GetUniqueTestEmail()
    {
        return $"dbtest-{Guid.NewGuid():N}@test.example.com";
    }

    [Fact]
    [FastTest]
    public async Task CreateTestUser_ViaUserManager_ShouldBeAccessibleInHttpRequests()
    {
        // Arrange
        var testEmail = GetUniqueTestEmail();

        // Act - Create user through test infrastructure (UserManager)
        var testUser = await CreateTestUserAsync(testEmail, TestPassword, emailVerified: true);

        // Verify database context consistency
        var actualDatabaseName = DatabaseContextValidationHelper.GetActualDatabaseName(ServiceScope.ServiceProvider);
        Assert.Equal(DatabaseName, actualDatabaseName);

        // Now try to login via HTTP API - this should work if database sharing is fixed
        var loginRequest = new
        {
            Email = testEmail,
            Password = TestPassword,
            RememberMe = false
        };

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Get CSRF token
        var csrfResponse = await Client.GetAsync("/api/auth/csrf-token");
        csrfResponse.EnsureSuccessStatusCode();
        var csrfJson = await csrfResponse.Content.ReadAsStringAsync();
        var csrfData = JsonSerializer.Deserialize<JsonElement>(csrfJson);
        var csrfToken = csrfData.GetProperty("token").GetString();

        content.Headers.Add("X-CSRF-TOKEN", csrfToken);

        // Act - Login via HTTP request
        var response = await Client.PostAsync("/api/auth/login", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
        Assert.True(loginResponse.GetProperty("success").GetBoolean());
        // Cookie-based authentication - no access token in response
        Assert.Equal(testEmail, loginResponse.GetProperty("user").GetProperty("email").GetString());

        // CRITICAL VALIDATION: User should be found in both contexts
        var userInTestContext = await Context.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
        Assert.NotNull(userInTestContext);

        // Verify database isolation - no cross-contamination
        var allUsers = await Context.Users.ToListAsync();
        Assert.Contains(allUsers, u => u.Email == testEmail); // Should contain our test user
        // Note: Standard users from SimpleTestDataSeeder are expected in shared factory
    }

    [Fact]
    [FastTest]
    public async Task MultipleTests_ShouldUseIsolatedDatabases()
    {
        // Arrange
        var testEmail1 = GetUniqueTestEmail();
        var testEmail2 = GetUniqueTestEmail();

        // Act - Create users in different test instances
        var testUser1 = await CreateTestUserAsync(testEmail1, TestPassword, emailVerified: true);

        // Create a separate test scope to simulate isolation
        using var separateFactory = new SharedWebApplicationFactory();

        // Set up separate database for second test - use instance-specific method
        var separateDatabaseName = $"SeparateTest_{Guid.NewGuid():N}";
        separateFactory.SetInstanceDatabaseName(separateDatabaseName);

        using var separateClient = separateFactory.CreateClient();
        using var separateScope = separateFactory.Services.CreateScope();
        var separateContext = separateScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

        // The separate context should not see the first user
        var userInSeparateContext = await separateContext.Users.FirstOrDefaultAsync(u => u.Email == testEmail1);
        Assert.Null(userInSeparateContext);

        // Create second user in separate context
        var separateUserManager = separateScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var secondUser = new User
        {
            Email = testEmail2,
            UserName = testEmail2,
            EmailConfirmed = true,
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await separateUserManager.CreateAsync(secondUser, TestPassword);
        Assert.True(createResult.Succeeded);

        // Verify isolation - each context only sees its own user
        var usersInOriginalContext = await Context.Users.Where(u => u.Email == testEmail1 || u.Email == testEmail2).ToListAsync();
        var usersInSeparateContext = await separateContext.Users.Where(u => u.Email == testEmail1 || u.Email == testEmail2).ToListAsync();

        Assert.Single(usersInOriginalContext); // Only first user
        Assert.Single(usersInSeparateContext); // Only second user
        Assert.Equal(testEmail1, usersInOriginalContext[0].Email);
        Assert.Equal(testEmail2, usersInSeparateContext[0].Email);
    }

}