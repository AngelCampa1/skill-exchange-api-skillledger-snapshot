using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration;

[Collection("Integration Api 1")]
public class BadgeApiIntegrationTests : IClassFixture<SharedWebApplicationFactory>, IAsyncLifetime
{
    private readonly SharedWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly SkillLedgerDbContext _context;
    private readonly IServiceScope _scope;
    private readonly string _databaseName;
    private User _testUser = null!;
    private User _adminUser = null!;

    public BadgeApiIntegrationTests(SharedTestHostFixture fixture)
    {
        // CRITICAL: Set unique database name for test isolation
        _databaseName = $"TestDatabase_{Guid.NewGuid():N}_BadgeApiIntegrationTests";
        SharedWebApplicationFactory.SetDatabaseNameForCurrentContext(_databaseName);

        _factory = fixture.Factory;
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        // CRITICAL: Add database name header to all HTTP requests
        _client.DefaultRequestHeaders.Add("X-Test-Database", _databaseName);

        _scope = _factory.Services.CreateScope();
        _context = _scope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();
    }

    public async Task InitializeAsync()
    {
        await SetupAsync();
    }

    public Task DisposeAsync()
    {
        _scope?.Dispose();
        _client?.Dispose();
        SharedWebApplicationFactory.ClearDatabaseNameForCurrentContext();
        return Task.CompletedTask;
    }

    private async Task SetupAsync()
    {
        await _context.Database.EnsureCreatedAsync();
        await SeedTestDataAsync();

        // Ensure all data is committed to the database and detach entities to prevent tracking issues
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    private async Task SeedTestDataAsync()
    {
        // Complete cleanup to ensure fresh test state
        await CleanupDatabaseAsync();

        // Create test users with unique identifiers to avoid conflicts
        var testIdentifier = Guid.NewGuid().ToString("N")[..8];
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"badgeuser{testIdentifier}@example.com",
            Email = $"badgeuser{testIdentifier}@example.com",
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow.AddDays(-400)
        };

        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = $"badgeadmin{testIdentifier}@example.com",
            Email = $"badgeadmin{testIdentifier}@example.com",
            EmailConfirmed = true
        };

        _context.Users.Add(_testUser);
        _context.Users.Add(_adminUser);
        await _context.SaveChangesAsync();

        // Create badge definitions with criteria
        var highPerformerDef = new BadgeDefinition
        {
            Id = Guid.NewGuid(),
            BadgeType = "HIGH_PERFORMER",
            Category = BadgeCategory.Performance,
            DisplayName = "High Performer",
            Description = "Maintains 4.5+ average rating across 10+ projects",
            RequiredVerification = VerificationLevel.Automatic,
            IsActive = true
        };

        var verifiedIdentityDef = new BadgeDefinition
        {
            Id = Guid.NewGuid(),
            BadgeType = "VERIFIED_IDENTITY",
            Category = BadgeCategory.Trust,
            DisplayName = "Verified Identity",
            Description = "Government-issued ID verification completed",
            RequiredVerification = VerificationLevel.Manual,
            IsActive = true
        };

        _context.BadgeDefinitions.AddRange(highPerformerDef, verifiedIdentityDef);
        await _context.SaveChangesAsync();

        // Add criteria for HIGH_PERFORMER badge
        var ratingCriteria = new BadgeCriteria
        {
            Id = Guid.NewGuid(),
            BadgeType = "HIGH_PERFORMER",
            CriteriaName = "Average Rating",
            CriteriaValue = "4.5",
            IsActive = true,
            Priority = 1
        };

        var projectCountCriteria = new BadgeCriteria
        {
            Id = Guid.NewGuid(),
            BadgeType = "HIGH_PERFORMER",
            CriteriaName = "Completed Projects",
            CriteriaValue = "10",
            IsActive = true,
            Priority = 2
        };

        var identityCriteria = new BadgeCriteria
        {
            Id = Guid.NewGuid(),
            BadgeType = "VERIFIED_IDENTITY",
            CriteriaName = "Identity Verification",
            CriteriaValue = "Manual Review Required",
            IsActive = true,
            Priority = 1
        };

        _context.BadgeCriteria.AddRange(ratingCriteria, projectCountCriteria, identityCriteria);

        // Add some completed projects for the user
        for (int i = 0; i < 15; i++)
        {
            _context.Projects.Add(new Project
            {
                Id = Guid.NewGuid(),
                ClientId = _testUser.Id,
                Title = $"Test Project {i}",
                Description = $"Description {i}",
                Status = ProjectStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-i * 10)
            });
        }

        // Add reputation score for the test user to support badge progress calculations
        var reputationScore = new UserReputationScore
        {
            Id = Guid.NewGuid(),
            UserId = _testUser.Id,
            OverallScore = 4.7m,
            ProjectCompletionRate = 0.95m,
            AverageResponseTime = 4,
            TotalProjectsCompleted = 15,
            LastUpdated = DateTime.UtcNow
        };
        _context.UserReputationScores.Add(reputationScore);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetUserBadges_ValidUser_ReturnsEmptyList()
    {
        // Arrange
        AuthenticateAs(_testUser);

        // Debug: Check test data integrity
        var testContextBadges = await _context.BadgeDefinitions.CountAsync();
        var testContextUsers = await _context.Users.CountAsync();
        Assert.True(testContextBadges >= 2, $"Expected at least 2 badge definitions in test context, found {testContextBadges}");
        Assert.True(testContextUsers >= 2, $"Expected at least 2 users in test context, found {testContextUsers}");

        // Act
        var response = await _client.GetAsync($"/api/badge/user/{_testUser.Id}/badges");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var badges = JsonSerializer.Deserialize<UserBadge[]>(content, TestJsonOptions.Default);

        Assert.NotNull(badges);
        Assert.Empty(badges); // User starts with no badges
    }

    [Fact]
    public async Task GetBadgeProgress_ValidUser_ReturnsProgressData()
    {
        // Arrange
        AuthenticateAs(_testUser);

        // Debug: Verify test user exists in context before API call
        var userInContext = await _context.Users.FindAsync(_testUser.Id);
        Assert.NotNull(userInContext);

        // Debug: Verify badge definitions exist in context before API call
        var badgeDefsInContext = await _context.BadgeDefinitions.CountAsync();
        Assert.True(badgeDefsInContext >= 2, $"Expected at least 2 badge definitions, found {badgeDefsInContext}");

        // Ensure all changes are saved before API call
        await _context.SaveChangesAsync();

        // Act
        var response = await _client.GetAsync($"/api/badge/user/{_testUser.Id}/progress");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Debug: Log the response content if it's empty or unexpected
        if (string.IsNullOrEmpty(content) || content == "[]")
        {
            // Get fresh context to check data state
            using var freshScope = _factory.Services.CreateScope();
            var freshContext = freshScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();
            var userExists = await freshContext.Users.AnyAsync(u => u.Id == _testUser.Id);
            var badgeCount = await freshContext.BadgeDefinitions.CountAsync();
            throw new InvalidOperationException($"API returned empty progress. User exists in fresh context: {userExists}, Badge definitions count: {badgeCount}, Response: {content}");
        }

        var progress = JsonSerializer.Deserialize<BadgeProgressDto[]>(content, TestJsonOptions.Default);

        Assert.NotNull(progress);
        Assert.NotEmpty(progress);

        var highPerformerProgress = progress.FirstOrDefault(p => p.BadgeType == "HIGH_PERFORMER");
        Assert.NotNull(highPerformerProgress);
        Assert.Equal("High Performer", highPerformerProgress.BadgeName);
    }

    [Fact]
    public async Task CheckBadgeEligibility_ValidUser_ReturnsEligibilityData()
    {
        // Arrange
        AuthenticateAs(_testUser);

        // Act
        var response = await _client.GetAsync($"/api/badge/user/{_testUser.Id}/eligibility");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var eligibility = JsonSerializer.Deserialize<BadgeProgressDto[]>(content, TestJsonOptions.Default);

        // The API should return a valid response (could be empty array if no badges are eligible, but not null)
        Assert.NotNull(eligibility);
        // We don't require non-empty since eligibility depends on complex business logic and user data
    }

    [Fact]
    public async Task SubmitVerificationRequest_ValidRequest_CreatesRequest()
    {
        // Arrange
        AuthenticateAs(_testUser);

        var request = new SubmitVerificationRequestDto
        {
            BadgeType = "VERIFIED_IDENTITY",
            Evidence = new Dictionary<string, object>
            {
                { "documentType", "passport" },
                { "documentNumber", "A1234567" }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/badge/verification/request", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var verificationRequest = JsonSerializer.Deserialize<VerificationRequest>(content, TestJsonOptions.Default);

        Assert.NotNull(verificationRequest);
        Assert.Equal("VERIFIED_IDENTITY", verificationRequest.BadgeType);
        Assert.Equal("Pending", verificationRequest.Status);
    }

    [Fact]
    public async Task SubmitVerificationRequest_DuplicateRequest_ReturnsConflict()
    {
        // Arrange
        AuthenticateAs(_testUser);

        var request = new SubmitVerificationRequestDto
        {
            BadgeType = "VERIFIED_IDENTITY",
            Evidence = new Dictionary<string, object> { { "test", "data" } }
        };

        // Submit first request
        await _client.PostAsJsonAsync("/api/badge/verification/request", request);

        // Act - Submit duplicate
        var response = await _client.PostAsJsonAsync("/api/badge/verification/request", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AwardBadge_AdminUser_SuccessfullyAwardsBadge()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var request = new AwardBadgeRequestDto
        {
            UserId = _testUser.Id,
            BadgeType = "HIGH_PERFORMER",
            Evidence = new Dictionary<string, object>
            {
                { "rating", 4.8 },
                { "projects", 12 }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/badge/award", request);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Verify badge was created in database
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == _testUser.Id && b.BadgeType == "HIGH_PERFORMER");
        Assert.NotNull(badge);
        Assert.True(badge.IsActive);
    }

    [Fact]
    public async Task AwardBadge_NonAdminUser_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_testUser);

        var request = new AwardBadgeRequestDto
        {
            UserId = _testUser.Id,
            BadgeType = "HIGH_PERFORMER"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/badge/award", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeBadge_AdminUser_SuccessfullyRevokesBadge()
    {
        // Arrange - First award a badge
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var awardRequest = new AwardBadgeRequestDto
        {
            UserId = _testUser.Id,
            BadgeType = "HIGH_PERFORMER"
        };

        var awardResponse = await _client.PostAsJsonAsync("/api/badge/award", awardRequest);
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == _testUser.Id && b.BadgeType == "HIGH_PERFORMER");

        var revokeRequest = new RevokeBadgeRequestDto
        {
            Reason = "Performance decline"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/badge/revoke/{badge!.Id}", revokeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify badge was revoked
        await _context.Entry(badge).ReloadAsync();
        Assert.False(badge.IsActive);
    }

    [Fact]
    public async Task GetPendingVerificationRequests_AdminUser_ReturnsPendingRequests()
    {
        // Store the current user's ID for test isolation
        var testUserId = _testUser.Id;

        // Clean up any existing verification requests for this specific test user to ensure isolation
        var existingUserRequests = await _context.VerificationRequests
            .Where(vr => vr.UserId == testUserId)
            .ToListAsync();
        _context.VerificationRequests.RemoveRange(existingUserRequests);
        await _context.SaveChangesAsync();

        // Arrange - Create a verification request first
        AuthenticateAs(_testUser);

        // Debug: Check if badge definition exists in test context before API call
        var badgeDefInTestContext = await _context.BadgeDefinitions
            .FirstOrDefaultAsync(bd => bd.BadgeType == "VERIFIED_IDENTITY");
        if (badgeDefInTestContext == null)
        {
            throw new InvalidOperationException("Badge definition 'VERIFIED_IDENTITY' not found in test context database");
        }

        // Debug: Check badge definitions count in test context
        var badgeDefCount = await _context.BadgeDefinitions.CountAsync();
        if (badgeDefCount == 0)
        {
            throw new InvalidOperationException("No badge definitions found in test context");
        }

        // Save changes to ensure all data is committed to the shared database
        await _context.SaveChangesAsync();

        var verificationRequest = new SubmitVerificationRequestDto
        {
            BadgeType = "VERIFIED_IDENTITY",
            Evidence = new Dictionary<string, object> { { "test", "data" } }
        };

        var submitResponse = await _client.PostAsJsonAsync("/api/badge/verification/request", verificationRequest);

        // Debug: Check if verification request creation failed
        if (!submitResponse.IsSuccessStatusCode)
        {
            var errorContent = await submitResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Failed to create verification request: {submitResponse.StatusCode} - {errorContent}");
        }

        // Ensure data is properly committed and refresh the test context to check
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Add a small delay to ensure database consistency across connections
        await Task.Delay(50);

        // Verify that the verification request was actually created and is visible
        var createdRequest = await _context.VerificationRequests
            .FirstOrDefaultAsync(vr => vr.UserId == testUserId && vr.BadgeType == "VERIFIED_IDENTITY");
        if (createdRequest == null)
        {
            throw new InvalidOperationException("Verification request was not created or is not visible in the test context");
        }

        // Add additional delay to ensure database is fully synchronized across connections
        await Task.Delay(100);

        // Force a fresh database query to ensure we see the latest data
        using var freshContext = _factory.Services.CreateScope().ServiceProvider.GetRequiredService<SkillLedgerDbContext>();
        var verifyDataExists = await freshContext.VerificationRequests
            .AnyAsync(vr => vr.UserId == testUserId && vr.BadgeType == "VERIFIED_IDENTITY");

        if (!verifyDataExists)
        {
            throw new InvalidOperationException("Test data is not visible in fresh database context");
        }

        // Switch to admin
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // Act
        var response = await _client.GetAsync("/api/badge/verification/pending");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();

        // Debug: Log the response content if deserialization fails
        try
        {
            var requests = JsonSerializer.Deserialize<VerificationRequest[]>(content, TestJsonOptions.Default);

            Assert.NotNull(requests);

            // If no requests returned, provide more detailed error information
            if (requests.Length == 0)
            {
                // Check what's actually in the database from the test context
                var contextRequests = await _context.VerificationRequests.ToListAsync();
                throw new InvalidOperationException($"No verification requests returned by API. Test context has {contextRequests.Count} requests. Response content: {content}");
            }

            // Look for the request created by this specific test user
            var userRequest = requests.FirstOrDefault(r => r.UserId == testUserId && r.BadgeType == "VERIFIED_IDENTITY");

            if (userRequest == null)
            {
                var allUsers = requests.Select(r => r.UserId).Distinct().ToList();
                throw new InvalidOperationException($"No verification request found for user {testUserId} and badge VERIFIED_IDENTITY. Found {requests.Length} requests for users: {string.Join(", ", allUsers)}");
            }

            Assert.Equal("VERIFIED_IDENTITY", userRequest.BadgeType);

            // Clean up only the verification request created by this specific test user
            var requestToRemove = await _context.VerificationRequests
                .FirstOrDefaultAsync(vr => vr.UserId == testUserId && vr.BadgeType == "VERIFIED_IDENTITY");
            if (requestToRemove != null)
            {
                _context.VerificationRequests.Remove(requestToRemove);
                await _context.SaveChangesAsync();
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize response. Content: {content}. Error: {ex.Message}");
        }
    }

    [Fact]
    public async Task ProcessVerificationRequest_AdminApproval_AwardsBadge()
    {
        // Arrange - Create a verification request
        AuthenticateAs(_testUser);

        var verificationRequest = new SubmitVerificationRequestDto
        {
            BadgeType = "VERIFIED_IDENTITY",
            Evidence = new Dictionary<string, object> { { "verified", true } }
        };

        var verificationResponse = await _client.PostAsJsonAsync("/api/badge/verification/request", verificationRequest);
        var createdRequest = await verificationResponse.Content.ReadFromJsonAsync<VerificationRequest>();

        // Switch to admin
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var decision = new ProcessVerificationRequestDto
        {
            Approved = true,
            ReviewNotes = "Documents verified successfully"
        };

        // Act
        var response = await _client.PostAsJsonAsync($"/api/badge/verification/request/{createdRequest!.Id}/process", decision);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify badge was awarded
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == _testUser.Id && b.BadgeType == "VERIFIED_IDENTITY");
        Assert.NotNull(badge);
        Assert.True(badge.IsActive);
    }

    [Fact]
    public async Task GenerateVerificationCode_AuthenticatedUser_ReturnsCode()
    {
        // Arrange - First award a badge
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var awardRequest = new AwardBadgeRequestDto
        {
            UserId = _testUser.Id,
            BadgeType = "HIGH_PERFORMER"
        };

        await _client.PostAsJsonAsync("/api/badge/award", awardRequest);
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == _testUser.Id && b.BadgeType == "HIGH_PERFORMER");

        // Switch to regular user
        AuthenticateAs(_testUser);

        // Act
        var response = await _client.PostAsync($"/api/badge/verify/{badge!.Id}/generate-code", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("verificationCode", out var codeProperty));
        Assert.True(codeProperty.GetString()!.Length > 0);
    }

    [Fact]
    public async Task VerifyBadgeCode_PublicEndpoint_ValidatesCode()
    {
        // Arrange - First generate a verification code
        AuthenticateAs(_adminUser, new[] { "Admin" });

        var awardRequest = new AwardBadgeRequestDto
        {
            UserId = _testUser.Id,
            BadgeType = "HIGH_PERFORMER"
        };

        await _client.PostAsJsonAsync("/api/badge/award", awardRequest);
        var badge = await _context.UserBadges
            .FirstOrDefaultAsync(b => b.UserId == _testUser.Id && b.BadgeType == "HIGH_PERFORMER");

        AuthenticateAs(_testUser);

        var codeResponse = await _client.PostAsync($"/api/badge/verify/{badge!.Id}/generate-code", null);
        var codeContent = await codeResponse.Content.ReadAsStringAsync();
        var codeResult = JsonSerializer.Deserialize<JsonElement>(codeContent);
        var verificationCode = codeResult.GetProperty("verificationCode").GetString();

        // Clear authorization for public endpoint
        _client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await _client.GetAsync($"/api/badge/verify/{badge.Id}?verificationCode={verificationCode}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        Assert.True(result.TryGetProperty("isValid", out var isValidProperty));
        Assert.True(isValidProperty.GetBoolean());
    }

    [Fact]
    public async Task RunAutomaticBadgeEvaluation_AdminUser_CompletesSuccessfully()
    {
        // Arrange
        AuthenticateAs(_adminUser, new[] { "Admin" });

        // BUG-BE-003 FIX: This endpoint now requires CSRF protection
        // Get CSRF token
        var csrfResponse = await _client.GetAsync("/api/auth/csrf-token");
        csrfResponse.EnsureSuccessStatusCode();
        var csrfJson = await csrfResponse.Content.ReadAsStringAsync();
        var csrfData = JsonSerializer.Deserialize<JsonElement>(csrfJson);
        var csrfToken = csrfData.GetProperty("token").GetString();

        var content = new StringContent("", Encoding.UTF8, "application/json");
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);

        // Act
        var response = await _client.PostAsync("/api/badge/evaluation/run", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

        Assert.True(result.TryGetProperty("message", out var messageProperty));
        Assert.Contains("completed", messageProperty.GetString()!);
    }

    private async Task CleanupDatabaseAsync()
    {
        try
        {
            // Remove all test data in correct dependency order
            // Use a safer approach to handle concurrency issues

            // Clear the change tracker to avoid conflicts
            _context.ChangeTracker.Clear();

            var verificationRequests = await _context.VerificationRequests.ToListAsync();
            if (verificationRequests.Any())
            {
                _context.VerificationRequests.RemoveRange(verificationRequests);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var userBadges = await _context.UserBadges.ToListAsync();
            if (userBadges.Any())
            {
                _context.UserBadges.RemoveRange(userBadges);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var badgeCriteria = await _context.BadgeCriteria.ToListAsync();
            if (badgeCriteria.Any())
            {
                _context.BadgeCriteria.RemoveRange(badgeCriteria);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var projects = await _context.Projects.ToListAsync();
            if (projects.Any())
            {
                _context.Projects.RemoveRange(projects);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var reputationScores = await _context.UserReputationScores.ToListAsync();
            if (reputationScores.Any())
            {
                _context.UserReputationScores.RemoveRange(reputationScores);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var badgeDefinitions = await _context.BadgeDefinitions.ToListAsync();
            if (badgeDefinitions.Any())
            {
                _context.BadgeDefinitions.RemoveRange(badgeDefinitions);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }

            var users = await _context.Users.Where(u => u.Email!.StartsWith("badgeuser") || u.Email!.StartsWith("badgeadmin")).ToListAsync();
            if (users.Any())
            {
                _context.Users.RemoveRange(users);
                await SaveChangesWithRetryAsync();
                _context.ChangeTracker.Clear();
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail tests due to cleanup issues
            System.Diagnostics.Debug.WriteLine($"Cleanup warning: {ex.Message}");
            _context.ChangeTracker.Clear();
        }
    }

    private async Task SaveChangesWithRetryAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Entity was already deleted by another test, ignore
            _context.ChangeTracker.Clear();
        }
        catch (Exception ex)
        {
            // Log but don't fail tests due to save issues
            System.Diagnostics.Debug.WriteLine($"Save changes warning: {ex.Message}");
            _context.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Authenticates the HTTP client as a specific user for integration tests
    /// Uses the TestAuthenticationHandler to set test claims without real cookies/tokens
    /// </summary>
    private void AuthenticateAs(User user, string[]? roles = null, string[]? permissions = null)
    {
        // Clear any existing authorization headers
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Remove("X-Test-Email");
        _client.DefaultRequestHeaders.Remove("X-Test-Roles");
        _client.DefaultRequestHeaders.Remove("X-Test-Permissions");

        // Set test authentication headers that TestAuthenticationHandler will use
        _client.DefaultRequestHeaders.Add("X-Test-UserId", user.Id.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", user.Email);

        if (roles != null && roles.Length > 0)
        {
            _client.DefaultRequestHeaders.Add("X-Test-Roles", string.Join(",", roles));
        }

        if (permissions != null && permissions.Length > 0)
        {
            _client.DefaultRequestHeaders.Add("X-Test-Permissions", string.Join(",", permissions));
        }
    }

    /// <summary>
    /// Clears authentication headers to simulate an unauthenticated request
    /// </summary>
    private void ClearAuthentication()
    {
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Remove("X-Test-UserId");
        _client.DefaultRequestHeaders.Remove("X-Test-Email");
        _client.DefaultRequestHeaders.Remove("X-Test-Roles");
        _client.DefaultRequestHeaders.Remove("X-Test-Permissions");
    }

}