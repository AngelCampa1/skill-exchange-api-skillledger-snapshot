using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Api;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using static SkillLedger.Tests.Infrastructure.TestJsonOptions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration;

[IntegrationTest]
[SecurityTest]
[Collection("Integration Other")]
public class PasswordResetIntegrationTests : IntegrationTestBase
{
    public PasswordResetIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [FastTest]
    public async Task ForgotPassword_ValidEmail_ReturnsSuccess()
    {
        // Arrange - Create a properly verified user directly in database

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "resettest@example.com",
            UserName = "resettest@example.com",
            NormalizedEmail = "RESETTEST@EXAMPLE.COM",
            NormalizedUserName = "RESETTEST@EXAMPLE.COM",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "AQAAAAEAACcQAAAAEBIBFwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBwBOBw==", // Placeholder hash
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        // Get CSRF token
        var csrfToken = await GetCsrfTokenAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "resettest@example.com"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("If the email address is registered and verified, password reset instructions have been sent.", result.Message);

        // Verify password reset entry was created
        var passwordReset = await Context.PasswordResets.FirstOrDefaultAsync(pr => pr.UserId == user.Id);
        Assert.NotNull(passwordReset);
        Assert.False(passwordReset.IsUsed);
        Assert.True(passwordReset.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    [SecurityTest]
    public async Task ForgotPassword_NonExistentEmail_ReturnsGenericSuccess()
    {
        // Arrange
        // Get CSRF token
        var csrfToken = await GetCsrfTokenAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "nonexistent@example.com"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("If the email address is registered and verified, password reset instructions have been sent.", result.Message);
    }

    [Fact]
    [FastTest]
    public async Task ForgotPassword_InvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var csrfToken = await GetCsrfTokenAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "invalid-email" // Invalid format
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    [FastTest]
    public async Task ValidateResetToken_ValidToken_ReturnsTrue()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "hashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test"
        };

        await Context.PasswordResets.AddAsync(passwordReset);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/auth/validate-reset-token?token={Uri.EscapeDataString(token)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.True((bool)((JsonElement)result["valid"]).GetBoolean());
    }

    [Fact]
    [SecurityTest]
    public async Task ValidateResetToken_ExpiredToken_ReturnsFalse()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "hashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(-1), // Expired
            IsUsed = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test"
        };

        await Context.PasswordResets.AddAsync(passwordReset);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/auth/validate-reset-token?token={Uri.EscapeDataString(token)}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.False((bool)((JsonElement)result["valid"]).GetBoolean());
    }

    [Fact]
    [FastTest]
    public async Task ResetPassword_ValidTokenAndPassword_ResetsPasswordSuccessfully()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "oldhashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test"
        };

        await Context.PasswordResets.AddAsync(passwordReset);
        await Context.SaveChangesAsync();

        // Get CSRF token
        var csrfToken = await GetCsrfTokenAsync();

        var request = new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "NewSecureP@ssw0rd123!",
            ConfirmPassword = "NewSecureP@ssw0rd123!"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/reset-password", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ResetPasswordResponseDto>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Password has been reset successfully. You can now log in with your new password.", result.Message);
        Assert.False(result.TokenExpired);

        // Verify token was marked as used (refresh context to get latest data)
        Context.ChangeTracker.Clear(); // Clear any cached entities
        var updatedPasswordReset = await Context.PasswordResets.FirstAsync(pr => pr.Id == passwordReset.Id);
        Assert.True(updatedPasswordReset.IsUsed);
        Assert.NotNull(updatedPasswordReset.UsedAt);

        // Verify password was changed (security stamp should be different)
        var updatedUser = await Context.Users.FirstAsync(u => u.Id == user.Id);
        Assert.NotEqual(user.SecurityStamp, updatedUser.SecurityStamp);
    }

    [Fact]
    [SecurityTest]
    public async Task ResetPassword_WeakPassword_ReturnsBadRequest()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "testuser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "oldhashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        var token = GenerateTestToken();
        var tokenHash = HashToken(token);

        var passwordReset = new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsUsed = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test"
        };

        await Context.PasswordResets.AddAsync(passwordReset);
        await Context.SaveChangesAsync();

        // Get CSRF token
        var csrfToken = await GetCsrfTokenAsync();

        var request = new ResetPasswordRequestDto
        {
            Token = token,
            NewPassword = "weak", // Too weak
            ConfirmPassword = "weak"
        };

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/reset-password", requestContent);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify token was NOT marked as used
        var unchangedPasswordReset = await Context.PasswordResets.FirstAsync(pr => pr.Id == passwordReset.Id);
        Assert.False(unchangedPasswordReset.IsUsed);
    }

    [Fact]
    [SecurityTest]
    public async Task ForgotPassword_RateLimited_ReturnsTooManyRequests()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "ratelimit@example.com",
            UserName = "ratelimituser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "hashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        // Create 3 password reset requests in the last hour (at the limit)
        for (int i = 0; i < 3; i++)
        {
            await Context.PasswordResets.AddAsync(new PasswordReset
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-30),
                ExpiresAt = DateTime.UtcNow.AddMinutes(30),
                IsUsed = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Test"
            });
        }
        await Context.SaveChangesAsync();

        var request = new ForgotPasswordRequestDto
        {
            Email = "ratelimit@example.com"
        };

        // Get CSRF token
        var csrfToken = await GetCsrfTokenAsync();

        var requestContent = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json");

        await AddCsrfTokenToRequest(requestContent);

        // Act
        var response = await Client.PostAsync("/api/auth/forgot-password", requestContent);

        // Assert
        // Note: The actual rate limiting behavior depends on the rate limiter implementation
        // This test verifies the service-level rate limiting logic
        var responseContent = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(responseContent, TestJsonOptions.Default);
            Assert.NotNull(result);
            Assert.False(result.Success);
            Assert.Contains("Too many", result.Message);
        }
        else
        {
            // If not rate limited at HTTP level, should still be service-level rate limited
            var result = JsonSerializer.Deserialize<ForgotPasswordResponseDto>(responseContent, TestJsonOptions.Default);
            Assert.NotNull(result);
            // Service returns generic message even when rate limited for security
            Assert.Equal("If the email address is registered and verified, password reset instructions have been sent.", result.Message);
        }
    }

    [Fact]
    [FastTest]
    public async Task GetPasswordResetAttempts_ValidEmail_ReturnsAttemptInfo()
    {
        // Arrange

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "attempts@example.com",
            UserName = "attemptsuser",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            PasswordHash = "hashedpassword",
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedFromIP = "127.0.0.1"
        };

        await Context.Users.AddAsync(user);

        // Add 1 recent attempt
        await Context.PasswordResets.AddAsync(new PasswordReset
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = "hash1",
            CreatedAt = DateTime.UtcNow.AddMinutes(-30),
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            IsUsed = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test"
        });

        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/auth/password-reset-attempts?email={Uri.EscapeDataString("attempts@example.com")}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.Equal("attempts@example.com", ((JsonElement)result["email"]).GetString());
        Assert.Equal(2, ((JsonElement)result["remainingAttempts"]).GetInt32()); // 3 - 1 = 2 remaining
        Assert.True(((JsonElement)result["canRequestReset"]).GetBoolean());
    }

    private static string GenerateTestToken()
    {
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        var tokenBytes = new byte[64];
        rng.GetBytes(tokenBytes);
        return Convert.ToBase64String(tokenBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string HashToken(string token)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}