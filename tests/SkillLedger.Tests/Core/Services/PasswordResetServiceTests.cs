using SkillLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using Xunit;
using System.Security.Cryptography;
using System.Text;

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[SecurityTest]
public class PasswordResetServiceTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<IAuditLogService> _mockAuditLogService;
    private readonly Mock<ILogger<PasswordResetService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly PasswordResetService _passwordResetService;

    public PasswordResetServiceTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new SkillLedgerDbContext(options);
        _mockEmailService = new Mock<IEmailService>();
        _mockUserService = new Mock<IUserService>();
        _mockAuditLogService = new Mock<IAuditLogService>();
        _mockLogger = new Mock<ILogger<PasswordResetService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        var mockLockService = new Mock<IDistributedLockService>();

        // Setup configuration
        _mockConfiguration.Setup(x => x["App:BaseUrl"]).Returns("https://test.com");

        // Setup distributed lock service to always succeed
        var mockDistributedLock = new Mock<IDistributedLock>();
        mockDistributedLock.Setup(x => x.IsAcquired).Returns(true);
        mockDistributedLock.Setup(x => x.Resource).Returns("test-resource");
        mockDistributedLock.Setup(x => x.AcquiredAt).Returns(DateTime.UtcNow);
        mockDistributedLock.Setup(x => x.ExpiresAt).Returns(DateTime.UtcNow.AddMinutes(5));
        mockDistributedLock.Setup(x => x.ExtendAsync(It.IsAny<TimeSpan>())).ReturnsAsync(true);
        mockDistributedLock.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        mockLockService
            .Setup(x => x.TryAcquireLockAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .ReturnsAsync(mockDistributedLock.Object);

        _passwordResetService = new PasswordResetService(
            _context,
            _mockEmailService.Object,
            _mockUserService.Object,
            _mockAuditLogService.Object,
            _mockLogger.Object,
            _mockConfiguration.Object,
            mockLockService.Object
        );
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_ValidEmailWithVerifiedUser_SendsEmailAndReturnsSuccess()
    {
        // Arrange
        var email = "test@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser",
            EmailConfirmed = true,
            Status = UserStatus.Active
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        _mockEmailService
            .Setup(x => x.SendPasswordResetEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        _mockAuditLogService
            .Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.InitiatePasswordResetAsync(email, ipAddress, userAgent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("If the email address is registered and verified, password reset instructions have been sent.", result.Message);

        // Verify email was sent
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(
            email,
            user.UserName,
            It.IsAny<string>(),
            "https://test.com"), Times.Once);

        // Verify audit log
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            user.Id,
            "PASSWORD_RESET_REQUESTED",
            ipAddress,
            userAgent,
            true,
            "Password reset email sent successfully",
            null), Times.Once);

        // Verify database entry
        var resetRequest = await _context.PasswordResets.FirstOrDefaultAsync(pr => pr.UserId == user.Id);
        Assert.NotNull(resetRequest);
        Assert.False(resetRequest.IsUsed);
        Assert.True(resetRequest.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task InitiatePasswordResetAsync_NonExistentEmail_ReturnsGenericSuccessMessage()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        _mockAuditLogService
            .Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.InitiatePasswordResetAsync(email, ipAddress, userAgent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("If the email address is registered and verified, password reset instructions have been sent.", result.Message);

        // Verify no email was sent
        _mockEmailService.Verify(x => x.SendPasswordResetEmailAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);

        // Verify audit log for invalid email
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            null,
            "PASSWORD_RESET_INVALID_EMAIL",
            ipAddress,
            userAgent,
            false,
            $"Reset requested for non-existent/unconfirmed email: {email}",
            null), Times.Once);
    }


    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task ValidateResetTokenAsync_InvalidToken_ReturnsFalse(string? token)
    {
        // Act
        var result = await _passwordResetService.ValidateResetTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var resetRequest = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = string.Empty, // Token should be cleared after email
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        await _context.PasswordResets.AddAsync(resetRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.ValidateResetTokenAsync(token);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateResetTokenAsync_ExpiredToken_ReturnsFalse()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var resetRequest = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false
        };

        await _context.PasswordResets.AddAsync(resetRequest);
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.ValidateResetTokenAsync(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CompletePasswordResetAsync_ValidTokenAndPassword_ResetsPasswordSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var resetRequest = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            AttemptCount = 0
        };

        await _context.PasswordResets.AddAsync(resetRequest);
        await _context.SaveChangesAsync();

        var newPassword = "NewSecureP@ssw0rd123!";
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        _mockUserService
            .Setup(x => x.UpdatePasswordAsync(user.Id, newPassword))
            .ReturnsAsync(new ServiceResponseDto { Success = true, Message = "Password updated successfully." });

        _mockAuditLogService
            .Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.CompletePasswordResetAsync(token, newPassword, ipAddress, userAgent);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Password has been reset successfully. You can now log in with your new password.", result.Message);

        // Verify password update was called
        _mockUserService.Verify(x => x.UpdatePasswordAsync(user.Id, newPassword), Times.Once);

        // Verify token was marked as used
        var updatedResetRequest = await _context.PasswordResets.FirstAsync(pr => pr.Id == resetRequest.Id);
        Assert.True(updatedResetRequest.IsUsed);
        Assert.NotNull(updatedResetRequest.UsedAt);

        // Verify audit log
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            user.Id,
            "PASSWORD_RESET_COMPLETED",
            ipAddress,
            userAgent,
            true,
            "Password reset completed successfully",
            null), Times.Once);
    }

    [Fact]
    public async Task CompletePasswordResetAsync_ExpiredToken_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var resetRequest = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false,
            AttemptCount = 0
        };

        await _context.PasswordResets.AddAsync(resetRequest);
        await _context.SaveChangesAsync();

        var newPassword = "NewSecureP@ssw0rd123!";
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        _mockAuditLogService
            .Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.CompletePasswordResetAsync(token, newPassword, ipAddress, userAgent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Reset token has expired. Please request a new password reset.", result.Message);

        // Verify password update was NOT called
        _mockUserService.Verify(x => x.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);

        // Verify audit log for expired token
        _mockAuditLogService.Verify(x => x.LogEventAsync(
            resetRequest.UserId,
            "PASSWORD_RESET_EXPIRED_TOKEN",
            ipAddress,
            userAgent,
            false,
            "Expired reset token used",
            null), Times.Once);
    }

    [Fact]
    public async Task CompletePasswordResetAsync_UsedToken_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var resetRequest = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true, // Already used
            UsedAt = DateTime.UtcNow.AddMinutes(-10),
            AttemptCount = 1
        };

        await _context.PasswordResets.AddAsync(resetRequest);
        await _context.SaveChangesAsync();

        var newPassword = "NewSecureP@ssw0rd123!";
        var ipAddress = "192.168.1.1";
        var userAgent = "Test Browser";

        _mockAuditLogService
            .Setup(x => x.LogEventAsync(
                It.IsAny<Guid?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _passwordResetService.CompletePasswordResetAsync(token, newPassword, ipAddress, userAgent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Reset token has already been used. Please request a new password reset.", result.Message);

        // Verify password update was NOT called
        _mockUserService.Verify(x => x.UpdatePasswordAsync(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CanRequestPasswordResetAsync_BelowRateLimit_ReturnsTrue()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Add only 2 recent attempts (below limit of 3)
        for (int i = 0; i < 2; i++)
        {
            await _context.PasswordResets.AddAsync(new PasswordReset
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.CanRequestPasswordResetAsync(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanRequestPasswordResetAsync_ExceedsRateLimit_ReturnsFalse()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Add 3 recent attempts (at the limit)
        for (int i = 0; i < 3; i++)
        {
            await _context.PasswordResets.AddAsync(new PasswordReset
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.CanRequestPasswordResetAsync(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CleanupExpiredTokensAsync_RemovesExpiredTokens_ReturnsCount()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);

        // Add expired tokens
        var expiredToken1 = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "expired1",
            CreatedAt = DateTime.UtcNow.AddHours(-3),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false
        };

        var expiredToken2 = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "expired2",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-1), // Expired
            IsUsed = false
        };

        // Add valid token
        var validToken = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "valid",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1), // Valid
            IsUsed = false
        };

        await _context.PasswordResets.AddRangeAsync(expiredToken1, expiredToken2, validToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.CleanupExpiredTokensAsync();

        // Assert
        Assert.Equal(2, result);

        // Verify only valid token remains
        var remainingTokens = await _context.PasswordResets.ToListAsync();
        Assert.Single(remainingTokens);
        Assert.Equal(validToken.Id, remainingTokens.First().Id);
    }

    [Fact]
    public async Task RevokeUserResetTokensAsync_RevokesActiveTokens_ReturnsCount()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser"
        };

        await _context.Users.AddAsync(user);

        // Add active tokens
        var activeToken1 = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "active1",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        var activeToken2 = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "active2",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false
        };

        // Add already used token (should not be affected)
        var usedToken = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "used",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = true,
            UsedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        await _context.PasswordResets.AddRangeAsync(activeToken1, activeToken2, usedToken);
        await _context.SaveChangesAsync();

        // Act
        var result = await _passwordResetService.RevokeUserResetTokensAsync(user.Id, "Test revocation");

        // Assert
        Assert.Equal(2, result);

        // Verify tokens were marked as used
        var tokens = await _context.PasswordResets.Where(pr => pr.UserId == user.Id).ToListAsync();
        var revokedTokens = tokens.Where(t => t.Id == activeToken1.Id || t.Id == activeToken2.Id).ToList();

        Assert.All(revokedTokens, token =>
        {
            Assert.True(token.IsUsed);
            Assert.NotNull(token.UsedAt);
        });

        // Verify previously used token unchanged
        var unchangedToken = tokens.First(t => t.Id == usedToken.Id);
        Assert.Equal(usedToken.UsedAt, unchangedToken.UsedAt);
    }

    private static string GenerateTestToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var tokenBytes = new byte[64];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}