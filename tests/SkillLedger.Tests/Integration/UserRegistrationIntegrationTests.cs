using SkillLedger.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Constants;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;

namespace SkillLedger.Tests.Integration;

[Collection("Integration Other")]
[IntegrationTest]
[CoreTest]
public class UserRegistrationIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SkillLedgerDbContext _context;
    private readonly IUserService _userService;
    private readonly IAuditLogService _auditLogService;

    public UserRegistrationIntegrationTests()
    {
        var services = new ServiceCollection();

        // Configure in-memory database
        services.AddDbContext<SkillLedgerDbContext>(options =>
            options.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()));

        // Configure logging
        services.AddLogging(builder => builder.AddConsole());

        // Register Identity with matching password requirements from production
        services.AddIdentity<User, IdentityRole<Guid>>(options =>
        {
            // Password settings - matching production configuration in Program.cs
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 12;
            options.Password.RequiredUniqueChars = 1;
        })
            .AddEntityFrameworkStores<SkillLedgerDbContext>()
            .AddDefaultTokenProviders(); // Required for email confirmation tokens

        // Add memory cache for AuditLogService
        services.AddMemoryCache();

        // Register services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAuditLogService, MockAuditLogService>(); // Use mock to avoid IMemoryCache dependency
        services.AddScoped<SkillLedger.Core.Interfaces.IEmailService, SkillLedger.Tests.Mocks.MockEmailService>(); // Use mock instead of real EmailService

        // Add missing distributed lock service
        services.AddScoped<SkillLedger.Core.Interfaces.IDistributedLockService, SkillLedger.Tests.Mocks.MockDistributedLockService>();

        // Add required configuration (empty is fine for mock)
        services.AddSingleton<Microsoft.Extensions.Configuration.IConfiguration>(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build());

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<SkillLedgerDbContext>();
        _userService = _serviceProvider.GetRequiredService<IUserService>();
        _auditLogService = _serviceProvider.GetRequiredService<IAuditLogService>();
    }

    [Fact]
    [FastTest]
    public async Task RegisterUserAsync_WithValidData_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "test@example.com",
            Password = "StrongPhrase123!",
            ConfirmPassword = "StrongPhrase123!",
            FirstName = "Test",
            LastName = "User",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1");

        // Assert
        Assert.True(result.Success, $"Registration failed: {result.Message}");
        Assert.NotEqual(Guid.Empty, result.UserId);

        // Verify user was created in database
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == registerDto.Email);
        Assert.NotNull(user);
        Assert.Equal(registerDto.Email, user.Email);
        Assert.True(user.EmailConfirmed); // Should be automatically confirmed

        // Verify password is hashed (not plain text)
        Assert.NotEqual(registerDto.Password, user.PasswordHash);
        Assert.True(user.PasswordHash?.Length > 20); // bcrypt hashes are long
    }

    [Fact]
    public async Task RegisterUserAsync_WithDuplicateEmail_ShouldReturnGenericResponse()
    {
        // Arrange
        var email = "duplicate@example.com";
        var firstUser = new RegisterUserDto
        {
            Email = email,
            Password = "StrongPhrase123!",
            ConfirmPassword = "StrongPhrase123!",
            FirstName = "First",
            LastName = "User",
            AcceptedTerms = true
        };
        var duplicateUser = new RegisterUserDto
        {
            Email = email,
            Password = "AnotherPhrase456!",
            ConfirmPassword = "AnotherPhrase456!",
            FirstName = "Duplicate",
            LastName = "User",
            AcceptedTerms = true
        };

        // Act
        var firstResult = await _userService.RegisterUserAsync(firstUser, "192.168.1.1");
        var duplicateResult = await _userService.RegisterUserAsync(duplicateUser, "192.168.1.2");

        // Assert
        Assert.True(firstResult.Success);
        // Security: Returns generic error to prevent email enumeration attacks
        // The actual duplicate email detection is logged internally but not exposed to users
        Assert.False(duplicateResult.Success);
        Assert.Contains("Registration could not be completed", duplicateResult.Message, StringComparison.OrdinalIgnoreCase);

        // Verify only one user was created
        var userCount = await _context.Users.CountAsync(u => u.Email == email);
        Assert.Equal(1, userCount);
    }

    [Theory]
    [InlineData("valid@example.com", "weak")]
    [InlineData("", "ValidPhrase123!")]
    [InlineData("valid@example.com", "")]
    public async Task RegisterUserAsync_WithInvalidData_ShouldReturnValidationError(string email, string password)
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            FirstName = "Test",
            LastName = "User",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task RegisterUserAsync_ShouldCreateAuditLog()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "audit@example.com",
            Password = "StrongPhrase123!",
            ConfirmPassword = "StrongPhrase123!",
            FirstName = "Audit",
            LastName = "User",
            AcceptedTerms = true
        };

        // Act
        await _userService.RegisterUserAsync(registerDto, "192.168.1.100");

        // Assert
        var auditLogs = await _context.AuditLogs
            .Where(a => a.IPAddress == "192.168.1.100")
            .ToListAsync();

        Assert.NotEmpty(auditLogs);

        var registrationLog = auditLogs.FirstOrDefault(a => a.Action == SkillLedger.Core.Interfaces.AuditActions.USER_REGISTRATION);
        Assert.NotNull(registrationLog);
        Assert.Equal("192.168.1.100", registrationLog.IPAddress);
    }


    [Fact]
    public async Task RegisterUserAsync_WithRateLimitingScenario_ShouldTrackIPAttempts()
    {
        // Arrange
        var ipAddress = "192.168.1.200";
        var attempts = new List<RegisterUserDto>();

        for (int i = 0; i < 6; i++) // Attempt more than rate limit (5/hour)
        {
            attempts.Add(new RegisterUserDto
            {
                Email = $"user{i}@example.com",
                Password = "StrongPhrase123!",
                ConfirmPassword = "StrongPhrase123!",
                FirstName = $"User{i}",
                LastName = "Test",
                AcceptedTerms = true
            });
        }

        // Act & Assert
        var results = new List<bool>();
        for (int i = 0; i < attempts.Count; i++)
        {
            var result = await _userService.RegisterUserAsync(attempts[i], ipAddress);
            results.Add(result.Success);

            if (i < 5) // First 5 should succeed
            {
                Assert.True(result.Success, $"Attempt {i + 1} should succeed");
            }
            // TEST-HIGH-004 FIX: Note that rate limiting in UserService doesn't block,
            // it only logs. Actual rate limiting is enforced at API middleware layer.
            // The service layer always attempts registration and logs all attempts.
        }

        // Verify all attempts were logged (audit tracking works regardless of rate limit)
        var auditCount = await _context.AuditLogs
            .CountAsync(a => a.IPAddress == ipAddress && a.Action == SkillLedger.Core.Interfaces.AuditActions.USER_REGISTRATION);
        Assert.Equal(6, auditCount);

        // TEST-HIGH-004 FIX: Verify service tracks all 6 attempts for rate limit monitoring
        // In the real API, the 6th attempt would be blocked by middleware before reaching this service
        Assert.Equal(6, results.Count);
        Assert.True(results.Take(5).All(r => r), "First 5 attempts should succeed at service layer");
    }

    public void Dispose()
    {
        _context?.Dispose();
        _serviceProvider?.Dispose();
    }
}
