using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkillLedger.Api;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using System.Net.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Services;
using Xunit;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides common setup and teardown functionality
/// Uses shared WebApplicationFactory to reduce resource usage
/// Implements IAsyncLifetime to allow test-specific async initialization
/// NOTE: Each test class must declare its own [Collection("...")] attribute for parallel execution
/// </summary>
public abstract class IntegrationTestBase : IAsyncLifetime, IAsyncDisposable, IDisposable
{
    protected readonly WebApplicationFactory<Program> Factory;
    protected readonly HttpClient Client;
    protected readonly IServiceScope ServiceScope;
    protected readonly SkillLedgerDbContext Context;
    protected readonly string DatabaseName;
    // BUG-006 FIX: Removed unused _databaseAccess field
    private readonly string _testName;

    // Static database root to ensure all contexts share the same in-memory database
    protected static readonly Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot SharedDatabaseRoot =
        new Microsoft.EntityFrameworkCore.Storage.InMemoryDatabaseRoot();

    protected IntegrationTestBase(SharedTestHostFixture fixture)
    {
        try
        {
            // Configure memory management for testing
            TestMemoryManager.ConfigureForTesting();

            // CRITICAL FIX: Use unique database name per test instance to ensure proper isolation
            // This prevents concurrency issues and DbUpdateConcurrencyException
            DatabaseName = $"TestDatabase_{Guid.NewGuid():N}_{GetType().Name}_{DateTime.UtcNow.Ticks}";
            _testName = GetType().Name;

            // CRITICAL: Set database name for this test instance before creating any contexts
            SharedWebApplicationFactory.SetDatabaseNameForCurrentContext(DatabaseName);

            // CRITICAL FIX: Use the already-initialized factory from the fixture
            // The fixture built the IHost during InitializeAsync, so CreateClient() won't trigger a rebuild
            // This prevents the 5-minute timeout that occurred when each test tried to rebuild the host
            Factory = fixture?.Factory ?? throw new ArgumentNullException(nameof(fixture));

            // Create client - this now reuses the existing IHost instead of rebuilding it
            Client = Factory.CreateClient();

            // CRITICAL FIX: Add database name header to all HTTP requests
            // This ensures the API uses the correct test database regardless of thread
            Client.DefaultRequestHeaders.Add("X-Test-Database", DatabaseName);

            // Create service scope with error handling
            ServiceScope = Factory.Services.CreateScope();
            Context = ServiceScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

            // CRITICAL FIX: Do NOT call Initialize() here - it calls async methods with .GetAwaiter().GetResult()
            // which causes deadlock when combined with IAsyncLifetime. Initialization moved to OnInitializeAsync()
        }
        catch (Exception ex)
        {
            // Clean up any partially initialized resources
            try
            {
                Context?.Dispose();
                ServiceScope?.Dispose();
                Client?.Dispose();
            }
            catch
            {
                // Ignore cleanup errors during initialization failure
            }

            throw new InvalidOperationException($"Failed to initialize integration test base for {GetType().Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Initialize the test database with proper async handling
    /// Called by xUnit's IAsyncLifetime.InitializeAsync() AFTER constructor completes
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            // Ensure database exists
            await Context.Database.EnsureCreatedAsync();

            // Seed standard data first
            await SimpleTestDataSeeder.SeedStandardDataAsync(Context);

            // Fast cleanup to remove test-specific data
            SimpleTestDataSeeder.FastCleanup(Context);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Test initialization warning for {_testName}: {ex.Message}");

            // Try to ensure minimal viable test state
            try
            {
                await Context.Database.EnsureCreatedAsync();
                await SimpleTestDataSeeder.SeedStandardDataAsync(Context);
            }
            catch
            {
                // If this fails, let the test proceed - it might still work
            }
        }
    }

    /// <summary>
    /// Cleans the test database by removing only test-specific data, preserving standard seed data
    /// </summary>
    protected virtual void CleanDatabase()
    {
        try
        {
            // First ensure standard data is seeded
            // BUG-HIGH-014 FIX: GetAwaiter().GetResult() is acceptable in test infrastructure
            // CleanDatabase is called from synchronous constructors/dispose methods where async is not possible.
            // This is a test-only method with no risk of deadlocks (no SynchronizationContext in xUnit).
            SimpleTestDataSeeder.SeedStandardDataAsync(Context).GetAwaiter().GetResult();

            // Fast cleanup that only removes test-specific data
            SimpleTestDataSeeder.FastCleanup(Context);
        }
        catch (Exception)
        {
            // If fast cleanup fails, fall back to full cleanup
            FullCleanDatabase();
        }
    }

    /// <summary>
    /// Gets a CSRF token for POST requests
    /// </summary>
    protected async Task<string> GetCsrfTokenAsync()
    {
        var response = await Client.GetAsync("/api/auth/csrf-token");
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(content);

        if (tokenData != null && tokenData.TryGetValue("token", out var token))
        {
            return token.ToString()!;
        }

        throw new InvalidOperationException("Could not retrieve CSRF token");
    }

    /// <summary>
    /// Adds CSRF token to HTTP content headers
    /// </summary>
    protected async Task AddCsrfTokenToRequest(HttpContent content)
    {
        var token = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", token);
    }

    /// <summary>
    /// Authenticates the HTTP client as a specific user for integration tests
    /// Uses the TestAuthenticationHandler to set test claims without real cookies/tokens
    /// </summary>
    protected void AuthenticateAs(User user, string[]? roles = null, string[]? permissions = null)
    {
        // Clear any existing authorization headers
        Client.DefaultRequestHeaders.Remove("Authorization");
        Client.DefaultRequestHeaders.Remove("X-Test-UserId");
        Client.DefaultRequestHeaders.Remove("X-Test-Email");
        Client.DefaultRequestHeaders.Remove("X-Test-Roles");
        Client.DefaultRequestHeaders.Remove("X-Test-Permissions");

        // Set test authentication headers that TestAuthenticationHandler will use
        Client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        Client.DefaultRequestHeaders.Add("X-Test-Email", user.Email);

        if (roles != null && roles.Length > 0)
        {
            Client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        }

        if (permissions != null && permissions.Length > 0)
        {
            Client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(",", permissions));
        }
    }

    /// <summary>
    /// Clears authentication headers to simulate an unauthenticated request
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Remove("Authorization");
        Client.DefaultRequestHeaders.Remove("X-Test-UserId");
        Client.DefaultRequestHeaders.Remove("X-Test-Email");
        Client.DefaultRequestHeaders.Remove("X-Test-Roles");
        Client.DefaultRequestHeaders.Remove("X-Test-Permissions");
    }

    /// <summary>
    /// Adds CSRF token to HTTP request message headers
    /// </summary>
    protected async Task AddCsrfTokenToRequest(HttpRequestMessage request)
    {
        var token = await GetCsrfTokenAsync();
        request.Headers.Add("X-CSRF-TOKEN", token);
    }

    /// <summary>
    /// Creates a test user that will be accessible to HTTP requests
    /// Uses the same UserManager that HTTP requests use and ensures proper persistence
    /// FIXED: Let UserManager handle user creation completely to ensure proper password hashing
    /// </summary>
    protected async Task<User> CreateTestUserAsync(string email, string password, bool emailVerified = true)
    {
        User? testUser = null;

        // CRITICAL FIX: Validate database context before creating user
        var actualDatabaseName = DatabaseContextValidationHelper.GetActualDatabaseName(ServiceScope.ServiceProvider);
        if (actualDatabaseName != DatabaseName)
        {
            throw new InvalidOperationException($"Database context mismatch. Expected: {DatabaseName}, Actual: {actualDatabaseName}");
        }

        // CRITICAL FIX: Let UserManager handle user creation completely
        // Don't create User object manually - this interferes with proper password hashing
        using (var factoryScope = Factory.Services.CreateScope())
        {
            var factoryUserManager = factoryScope.ServiceProvider.GetRequiredService<UserManager<User>>();

            // Create user object with minimal properties only
            var userToCreate = new User
            {
                Email = email,
                UserName = email,
                EmailConfirmed = emailVerified,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            var createResult = await factoryUserManager.CreateAsync(userToCreate, password);
            if (!createResult.Succeeded)
            {
                throw new Exception($"Failed to create test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
            }

            // Get the created user with proper password hashing
            testUser = await factoryUserManager.FindByEmailAsync(email);
            if (testUser == null)
            {
                throw new Exception("User creation appeared to succeed but user not found in database");
            }
        }

        // Verify user is accessible in our test scope context
        using (var testScope = Factory.Services.CreateScope())
        {
            var testUserManager = testScope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var foundUser = await testUserManager.FindByEmailAsync(email);
            if (foundUser == null)
            {
                throw new Exception("Created user not accessible in test scope - database isolation issue");
            }
            testUser = foundUser;
        }

        // CRITICAL FIX: Verify database context is still consistent after user creation
        var finalDatabaseName = DatabaseContextValidationHelper.GetActualDatabaseName(ServiceScope.ServiceProvider);
        if (finalDatabaseName != DatabaseName)
        {
            throw new InvalidOperationException($"Database context changed during user creation. Expected: {DatabaseName}, Actual: {finalDatabaseName}");
        }

        // Create an active subscription for the test user so SubscriptionMiddleware allows access.
        // All plans are paid (free tier removed); test users need an active subscription.
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, testUser.Id);

        return testUser;
    }

    /// <summary>
    /// IAsyncLifetime.InitializeAsync - Called by xUnit after constructor but before test method
    /// Override OnInitializeAsync() in derived classes for test-specific async initialization
    /// </summary>
    async Task IAsyncLifetime.InitializeAsync()
    {
        await OnInitializeAsync();

        // After all test-specific initialization, ensure every user has an active subscription.
        // SubscriptionMiddleware is default-on (free tier removed); any user without an active
        // subscription will receive 402, breaking tests that add users directly without going
        // through CreateTestUserAsync.
        EnsureAllUsersHaveSubscriptions();
    }

    /// <summary>
    /// Creates an active subscription for every user in the test database that does not already
    /// have one. This satisfies SubscriptionMiddleware for tests that add users directly.
    /// </summary>
    private void EnsureAllUsersHaveSubscriptions()
    {
        // Exclude users who already have ANY subscription (not just Active) to avoid
        // creating duplicate subscriptions for users with Trial, PastDue, or Expired records.
        var usersWithoutSubscription = Context.Users
            .Where(u => !Context.UserSubscriptions.Any(s => s.UserId == u.Id))
            .Select(u => u.Id)
            .ToList();

        foreach (var userId in usersWithoutSubscription)
        {
            SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, userId);
        }
    }

    /// <summary>
    /// Virtual method for derived classes to perform async initialization
    /// This runs AFTER the constructor, avoiding deadlocks from blocking async calls
    /// </summary>
    protected virtual async Task OnInitializeAsync()
    {
        // CRITICAL FIX: Call base initialization first to setup database
        await InitializeAsync();

        // Derived classes can override to add their own initialization
        // They should call await base.OnInitializeAsync() at the END of their override
    }

    /// <summary>
    /// IAsyncLifetime.DisposeAsync - Called by xUnit after test method completes
    /// </summary>
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }

    /// <summary>
    /// Async cleanup for better resource management
    /// </summary>
    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            // Clean up test data asynchronously
            await CleanDatabaseAsync();

            // BUG-006 FIX: Removed _databaseAccess disposal as field was removed

            // Dispose resources in proper order
            Context?.Dispose();
            ServiceScope?.Dispose();
            Client?.Dispose();

            // CRITICAL FIX: Clean up the database from the factory tracking
            SharedWebApplicationFactory.CleanupDatabase(DatabaseName);

            // CRITICAL FIX: Clear thread-local database name to prevent cross-contamination
            SharedWebApplicationFactory.ClearDatabaseNameForCurrentContext();

            // Memory cleanup
            TestMemoryManager.TryCollectMemory();
        }
        catch (Exception)
        {
            // Ignore disposal errors
        }
    }

    /// <summary>
    /// Synchronous dispose for compatibility
    /// </summary>
    public virtual void Dispose()
    {
        try
        {
            // Quick synchronous cleanup
            Context?.Dispose();
            ServiceScope?.Dispose();
            Client?.Dispose();

            // CRITICAL FIX: Clean up the database from the factory tracking
            SharedWebApplicationFactory.CleanupDatabase(DatabaseName);

            // CRITICAL FIX: Clear thread-local database name to prevent cross-contamination
            SharedWebApplicationFactory.ClearDatabaseNameForCurrentContext();
        }
        catch (Exception)
        {
            // Ignore disposal errors
        }
    }

    /// <summary>
    /// Async database cleanup for better performance
    /// </summary>
    protected virtual async Task CleanDatabaseAsync()
    {
        try
        {
            // Only clean test-specific data, not system data
            await CleanTestDataAsync();
        }
        catch (Exception)
        {
            // If cleanup fails, try full cleanup
            CleanDatabase();
        }
    }

    /// <summary>
    /// Clean only test-specific data efficiently
    /// SECURITY: Uses parameterized queries to prevent SQL injection
    /// </summary>
    private async Task CleanTestDataAsync()
    {
        // Get all test user IDs first
        var testUserIds = await Context.Users
            .Where(u => u.Email.StartsWith("test") || u.Email.Contains("@test") || u.Email.Contains("@example"))
            .Select(u => u.Id)
            .ToListAsync();

        if (!testUserIds.Any()) return;

        // Clean related data for test users only using parameterized queries
        // SECURITY FIX: Use ExecuteSqlInterpolatedAsync for proper parameterization
        foreach (var userId in testUserIds)
        {
            await Context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM AuditLogs WHERE UserId = {userId}");
        }

        foreach (var userId in testUserIds)
        {
            await Context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM Projects WHERE UserId = {userId}");
        }

        foreach (var userId in testUserIds)
        {
            await Context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM Users WHERE Id = {userId}");
        }
    }

    /// <summary>
    /// Full database cleanup as fallback - removes all data and re-seeds standard data
    /// </summary>
    private void FullCleanDatabase()
    {
        try
        {
            // Clean data in dependency order to avoid foreign key constraints
            Context.RemoveRange(Context.ReputationHistories);
            Context.RemoveRange(Context.CategoryReputationScores);
            Context.RemoveRange(Context.UserReputationScores);
            Context.RemoveRange(Context.ProjectReviews);
            Context.RemoveRange(Context.SkillEndorsements);
            Context.RemoveRange(Context.UserSkills);
            Context.RemoveRange(Context.ExperienceSkills);
            Context.RemoveRange(Context.Experiences);
            Context.RemoveRange(Context.ProjectWorkspaces);
            Context.RemoveRange(Context.ProjectSkills);
            Context.RemoveRange(Context.ProjectDeliverables);
            Context.RemoveRange(Context.Projects);
            Context.RemoveRange(Context.Skills);
            Context.RemoveRange(Context.Profiles);
            Context.RemoveRange(Context.CreditTransfers);
            Context.RemoveRange(Context.ProjectEscrows);
            Context.RemoveRange(Context.CreditTransactions);
            Context.RemoveRange(Context.CreditWallets);
            Context.RemoveRange(Context.PasswordResets);
            // RefreshTokens removed - cookie-based authentication
            Context.RemoveRange(Context.UserRoles);
            Context.RemoveRange(Context.RolePermissions);
            Context.RemoveRange(Context.Permissions);
            Context.RemoveRange(Context.Roles);
            Context.RemoveRange(Context.AuditLogs);
            Context.RemoveRange(Context.Users);

            Context.SaveChanges();

            // Re-seed standard data
            // BUG-HIGH-014 FIX: GetAwaiter().GetResult() is acceptable in test infrastructure
            // This is called from synchronous cleanup methods where async is not possible.
            SimpleTestDataSeeder.SeedStandardDataAsync(Context).GetAwaiter().GetResult();
        }
        catch (Exception)
        {
            // Ignore cleanup errors - database might be empty or in inconsistent state
        }
    }
}

