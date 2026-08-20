using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for PasswordResetService - Secure Password Reset Workflow.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (UserService, UserManager, AuditLog)
/// - Mocks only EXTERNAL services (Email, DistributedLock)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 2 (IEmailService, IDistributedLockService)
/// </summary>
public class PasswordResetServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly PasswordResetService _service;
    private readonly UserManager<User> _userManager;
    private readonly UserService _userService;
    private readonly MockAuditLogService _auditLogService;
    private readonly Mocks.MockEmailService _emailService;
    private readonly MockDistributedLockService _lockService;
    private readonly IConfiguration _configuration;

    // Test data
    private readonly User _testUser;
    private readonly string _testPassword = "TestPassword123!";
    private const string TestUserAgent = "Mozilla/5.0 Test Browser";

    public PasswordResetServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"PasswordResetTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup UserManager
        var userStore = new UserStore<User, Role, SkillLedgerDbContext, Guid>(_context);
        var userLogger = new LoggerFactory().CreateLogger<UserManager<User>>();
        _userManager = new UserManager<User>(
            userStore,
            null,
            new PasswordHasher<User>(),
            null,
            null,
            null,
            null,
            null,
            userLogger);

        // Setup mock services
        _auditLogService = new MockAuditLogService(_context);
        _emailService = new Mocks.MockEmailService();
        _lockService = new MockDistributedLockService();

        // Setup real UserService (internal service - should NOT be mocked)
        var creditWalletService = new Mocks.MockCreditWalletService(_context);
        var userServiceLogger = new LoggerFactory().CreateLogger<UserService>();
        _userService = new UserService(_context, _userManager, _auditLogService, _emailService, new NoOpSequencerClient(), creditWalletService, userServiceLogger);

        // Setup configuration for rate limiting and token expiration
        var configDict = new Dictionary<string, string>
        {
            { "App:BaseUrl", "https://localhost:3030" }
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict!)
            .Build();

        var logger = new LoggerFactory().CreateLogger<PasswordResetService>();

        _service = new PasswordResetService(
            _context,
            _emailService,
            _userService,
            _auditLogService,
            logger,
            _configuration,
            _lockService);

        // Create test user
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "resetuser",
            Email = "reset@example.com",
            FirstName = "Reset",
            LastName = "User",
            EmailConfirmed = true
        };

        var result = _userManager.CreateAsync(_testUser, _testPassword).Result;
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        _context.SaveChanges();
    }

    #region InitiatePasswordResetAsync Tests

    [Fact]
    public async Task InitiatePasswordResetAsync_ValidEmail_ShouldCreateTokenAndSendEmail()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Act
        var result = await _service.InitiatePasswordResetAsync(_testUser.Email!, ipAddress, TestUserAgent);

        // Assert - Generic success message (email enumeration protection)
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If the email address is registered");

        // Verify token created in database
        var token = await _context.PasswordResets
            .FirstOrDefaultAsync(t => t.UserId == _testUser.Id && !t.IsUsed);
        token.Should().NotBeNull();
        token!.ExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(5));
        token.AttemptCount.Should().Be(0);

        // Verify email sent
        _emailService.SentEmails.Should().ContainSingle();
        var email = _emailService.SentEmails.First();
        email.ToEmail.Should().Be(_testUser.Email);
        email.Subject.Should().Contain("Password Reset");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id &&
                                     a.Action.Contains("PASSWORD_RESET_REQUESTED"));
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_InvalidEmail_ShouldReturnGenericMessage()
    {
        // Arrange
        var invalidEmail = "nonexistent@example.com";
        var ipAddress = "192.168.1.1";

        // Act
        var result = await _service.InitiatePasswordResetAsync(invalidEmail, ipAddress, TestUserAgent);

        // Assert - Email enumeration protection: same message as valid email
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("If the email address is registered");

        // Verify no token created
        var tokenCount = await _context.PasswordResets.CountAsync();
        tokenCount.Should().Be(0);

        // Verify no email sent
        _emailService.SentEmails.Should().BeEmpty();
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_ExceedsRateLimit_ShouldReturnError()
    {
        // Arrange - Make 3 requests (max allowed per hour)
        for (int i = 0; i < 3; i++)
        {
            await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        }

        // Act - 4th request should fail
        var result = await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Too many password reset requests");
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_RevokesOldTokens_ShouldOnlyHaveOneActiveToken()
    {
        // Arrange - Create initial token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var firstTokenCount = await _context.PasswordResets
            .CountAsync(t => t.UserId == _testUser.Id && !t.IsUsed);

        firstTokenCount.Should().Be(1);

        // Act - Request another reset (should revoke first token)
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);

        // Assert - Only one active token should exist
        var activeTokens = await _context.PasswordResets
            .Where(t => t.UserId == _testUser.Id && !t.IsUsed)
            .ToListAsync();
        activeTokens.Should().ContainSingle("old tokens should be revoked when new one is created");
    }

    #endregion

    #region ValidateResetTokenAsync Tests

    [Fact]
    public async Task ValidateResetTokenAsync_ValidToken_ShouldReturnTrue()
    {
        // Arrange - Create valid token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var tokenEntity = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);

        // Get the actual token from email (since it's cleared from DB after sending)
        var sentEmail = _emailService.SentEmails.First();
        var tokenFromEmail = ExtractTokenFromEmail(sentEmail.Body);

        // Act
        var result = await _service.ValidateResetTokenAsync(tokenFromEmail);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateResetTokenAsync_ExpiredToken_ShouldReturnFalse()
    {
        // Arrange - Create token and manually expire it
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var token = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);

        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Expired 1 minute ago
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateResetTokenAsync(tokenFromEmail);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateResetTokenAsync_InvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid-token-12345";

        // Act
        var result = await _service.ValidateResetTokenAsync(invalidToken);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateResetTokenAsync_AlreadyUsedToken_ShouldReturnFalse()
    {
        // Arrange - Create and use token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var token = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);

        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);
        token.IsUsed = true;
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateResetTokenAsync(tokenFromEmail);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CompletePasswordResetAsync Tests

    [Fact]
    public async Task CompletePasswordResetAsync_ValidToken_ShouldResetPassword()
    {
        // Arrange - Create token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);

        var newPassword = "MyStr0ng!Phrase#2024";  // Strong password without weak patterns
        var ipAddress = "192.168.1.1";

        // Act
        var result = await _service.CompletePasswordResetAsync(tokenFromEmail, newPassword, ipAddress, TestUserAgent);

        // Assert
        result.Success.Should().BeTrue($"Password reset should succeed. Error: {result.Message}, ErrorDetails: {result.ErrorDetails}");
        result.Message.Should().Contain("Password has been reset");

        // Reload user from database to get updated password hash
        var updatedUser = await _context.Users.FindAsync(_testUser.Id);
        updatedUser.Should().NotBeNull();

        // Verify password changed
        var passwordCheck = await _userManager.CheckPasswordAsync(updatedUser!, newPassword);
        passwordCheck.Should().BeTrue();

        // Verify token marked as used
        var token = await _context.PasswordResets
            .FirstOrDefaultAsync(t => t.UserId == _testUser.Id);
        token.Should().NotBeNull();
        token!.IsUsed.Should().BeTrue();
    }

    [Fact]
    public async Task CompletePasswordResetAsync_ExpiredToken_ShouldReturnError()
    {
        // Arrange - Create expired token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);

        var token = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);
        token.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CompletePasswordResetAsync(tokenFromEmail, "NewPassword123!", "192.168.1.1", TestUserAgent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expired");

        // Verify password NOT changed
        var passwordCheck = await _userManager.CheckPasswordAsync(_testUser, _testPassword);
        passwordCheck.Should().BeTrue("original password should still work");
    }

    [Fact]
    public async Task CompletePasswordResetAsync_WeakPassword_ShouldReturnError()
    {
        // Arrange - Create token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);

        var weakPassword = "weak"; // Too short, no complexity

        // Act
        var result = await _service.CompletePasswordResetAsync(tokenFromEmail, weakPassword, "192.168.1.1", TestUserAgent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Password");

        // Verify token NOT marked as used yet (can retry with better password)
        var token = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);
        token.IsUsed.Should().BeFalse();
        token.AttemptCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task CompletePasswordResetAsync_DistributedLock_ShouldPreventConcurrentResets()
    {
        // Arrange - Create token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var tokenFromEmail = ExtractTokenFromEmail(_emailService.SentEmails.First().Body);

        // Acquire lock to simulate another process holding it
        var tokenHash = HashToken(tokenFromEmail);
        var existingLock = await _lockService.TryAcquireLockAsync(
            $"password_reset:{tokenHash}",
            TimeSpan.FromMinutes(5));

        // Act
        var result = await _service.CompletePasswordResetAsync(tokenFromEmail, "NewPassword123!", "192.168.1.1", TestUserAgent);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already in progress");

        // Cleanup
        if (existingLock != null)
        {
            await existingLock.DisposeAsync();
        }
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task GetRemainingResetAttemptsAsync_NewUser_ShouldReturn3()
    {
        // Act
        var remaining = await _service.GetRemainingResetAttemptsAsync(_testUser.Email!);

        // Assert
        remaining.Should().Be(3);
    }

    [Fact]
    public async Task GetRemainingResetAttemptsAsync_AfterRequests_ShouldDecrease()
    {
        // Arrange - Make 2 requests
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);

        // Act
        var remaining = await _service.GetRemainingResetAttemptsAsync(_testUser.Email!);

        // Assert
        remaining.Should().Be(1);
    }

    [Fact]
    public async Task CanRequestPasswordResetAsync_BelowLimit_ShouldReturnTrue()
    {
        // Arrange - Make 2 requests
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);

        // Act
        var canRequest = await _service.CanRequestPasswordResetAsync(_testUser.Email!);

        // Assert
        canRequest.Should().BeTrue();
    }

    [Fact]
    public async Task CanRequestPasswordResetAsync_AtLimit_ShouldReturnFalse()
    {
        // Arrange - Make 3 requests (max)
        for (int i = 0; i < 3; i++)
        {
            await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        }

        // Act
        var canRequest = await _service.CanRequestPasswordResetAsync(_testUser.Email!);

        // Assert
        canRequest.Should().BeFalse();
    }

    #endregion

    #region Token Management Tests

    [Fact]
    public async Task CleanupExpiredTokensAsync_ShouldRemoveOldTokens()
    {
        // Arrange - Create expired token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);
        var token = await _context.PasswordResets
            .FirstAsync(t => t.UserId == _testUser.Id);

        token.ExpiresAt = DateTime.UtcNow.AddDays(-2); // Expired 2 days ago
        await _context.SaveChangesAsync();

        // Act
        var cleanedCount = await _service.CleanupExpiredTokensAsync();

        // Assert
        cleanedCount.Should().BeGreaterThan(0);

        var tokenExists = await _context.PasswordResets
            .AnyAsync(t => t.Id == token.Id);
        tokenExists.Should().BeFalse("expired token should be deleted");
    }

    [Fact]
    public async Task RevokeUserResetTokensAsync_ShouldRevokeAllActiveTokens()
    {
        // Arrange - Create token
        await _service.InitiatePasswordResetAsync(_testUser.Email!, "192.168.1.1", TestUserAgent);

        // Act
        var revokedCount = await _service.RevokeUserResetTokensAsync(_testUser.Id, "Admin revocation");

        // Assert
        revokedCount.Should().Be(1);

        var activeTokens = await _context.PasswordResets
            .Where(t => t.UserId == _testUser.Id && !t.IsUsed)
            .ToListAsync();
        activeTokens.Should().BeEmpty("all tokens should be marked as used/revoked");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Extract token from password reset email (MockSentEmail has Token property)
    /// </summary>
    private string ExtractTokenFromEmail(string emailBody)
    {
        // MockSentEmail stores the token in its Token property
        // Get the most recent sent email
        var sentEmail = _emailService.SentEmails.Last();
        if (string.IsNullOrEmpty(sentEmail.Token))
        {
            throw new InvalidOperationException("Token not found in sent email");
        }
        return sentEmail.Token;
    }

    /// <summary>
    /// Hash token using SHA256 (matches service implementation)
    /// </summary>
    private string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _userManager.Dispose();
    }

    private sealed class NoOpSequencerClient : SkillLedger.Core.Interfaces.ISequencerClient
    {
        public Task EnrollAsync(
            string email,
            string sequenceSlug,
            string source,
            IReadOnlyDictionary<string, object?>? properties = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
