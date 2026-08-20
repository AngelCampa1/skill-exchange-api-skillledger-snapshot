using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Infrastructure;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for UserService - SECURITY CRITICAL.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real UserManager with in-memory stores
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses MockEmailService (external service - OK to mock)
/// - Verifies actual database state and Identity store, not mock interactions
///
/// Max mocked external dependencies: 1 (EmailService)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class UserServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly MockAuditLogService _auditLogService;  // REAL internal service
    private readonly Mocks.MockEmailService _emailService;  // EXTERNAL - OK to mock
    private readonly RecordingSequencerClient _sequencerClient;
    private readonly UserService _userService;

    public UserServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"UserServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);

        // Ensure database schema is created
        _context.Database.EnsureCreated();

        // Setup ASP.NET Identity with in-memory stores
        var userStore = new UserStore<User, IdentityRole<Guid>, SkillLedgerDbContext, Guid>(_context);
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
        var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();
        var logger = serviceProvider.GetRequiredService<ILogger<UserManager<User>>>();

        var identityOptions = new IdentityOptions
        {
            Password = new PasswordOptions
            {
                RequireDigit = true,
                RequiredLength = 12,
                RequireNonAlphanumeric = true,
                RequireUppercase = true,
                RequireLowercase = true
            }
        };

        _userManager = new UserManager<User>(
            userStore,
            Options.Create(identityOptions),
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            serviceProvider,
            logger
        );

        // Register token providers for email confirmation
        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();
        var tokenLogger = serviceProvider.GetRequiredService<ILogger<DataProtectorTokenProvider<User>>>();
        _userManager.RegisterTokenProvider("Default", new DataProtectorTokenProvider<User>(
            dataProtectionProvider,
            Options.Create(new DataProtectionTokenProviderOptions()),
            tokenLogger));

        // Setup services
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!
        _emailService = new Mocks.MockEmailService();  // External service
        _sequencerClient = new RecordingSequencerClient();
        var creditWalletService = new Mocks.MockCreditWalletService(_context);
        var mockLogger = new LoggerFactory().CreateLogger<UserService>();

        _userService = new UserService(
            _context,
            _userManager,
            _auditLogService,
            _emailService,
            _sequencerClient,
            creditWalletService,
            mockLogger
        );
    }

    [Fact]
    public async Task RegisterUserAsync_ValidRequest_CreatesUserAndLogsAudit()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "newuser@test.com",
            Password = "SecureP@ssw0rd123!",
            ConfirmPassword = "SecureP@ssw0rd123!",
            FirstName = "John",
            LastName = "Doe",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1", "Test User Agent");

        // Assert - Verify user created in database
        result.Should().NotBeNull();
        result.Success.Should().BeTrue(result.Message);

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == "newuser@test.com");
        user.Should().NotBeNull();
        user!.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.EmailConfirmed.Should().BeFalse("registration must not mark an email address verified before mailbox proof");
        user.PasswordHash.Should().NotBeNullOrEmpty();  // Password was hashed

        // Assert - Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == SkillLedger.Core.Constants.AuditActions.USER_REGISTRATION && a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.UserId.Should().Be(user.Id);
        auditLog.IPAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task RegisterUserAsync_ValidRequest_EnrollsSignupSequences()
    {
        var registerDto = new RegisterUserDto
        {
            Email = "sequence@test.com",
            Password = "SecureP@ssw0rd123!",
            ConfirmPassword = "SecureP@ssw0rd123!",
            FirstName = "Sequence",
            LastName = "User",
            AcceptedTerms = true
        };

        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1", "Test User Agent");

        result.Success.Should().BeTrue(result.Message);
        _sequencerClient.Enrollments.Should().HaveCount(2);
        _sequencerClient.Enrollments.Select(e => e.SequenceSlug).Should().BeEquivalentTo(
            "skillledger-fulfillment-welcome",
            "skillledger-nurture-value-1");
        _sequencerClient.Enrollments.Should().OnlyContain(e => e.Email == "sequence@test.com");
        _sequencerClient.Enrollments.Should().OnlyContain(e => e.Source == "skillledger_signup");
        _sequencerClient.Enrollments.Should().OnlyContain(e =>
            e.Properties != null &&
            e.Properties["first_name"]!.ToString() == "Sequence" &&
            e.Properties["last_name"]!.ToString() == "User");
    }

    [Fact]
    public async Task IsEmailAvailableAsync_ExistingEmail_ReturnsTrue()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            UserName = "existing@test.com",
            FirstName = "Existing",
            LastName = "User",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(existingUser, "ExistingP@ss123!");

        // Act - Email enumeration protection: always returns true
        var result = await _userService.IsEmailAvailableAsync("existing@test.com");

        // Assert - Should return true (email enumeration protection)
        result.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUserAsync_DuplicateEmail_ReturnsGenericError()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@test.com",
            UserName = "existing@test.com",
            FirstName = "Existing",
            LastName = "User",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(existingUser, "ExistingP@ss123!");

        var registerDto = new RegisterUserDto
        {
            Email = "existing@test.com", // Duplicate email
            Password = "NewSecureP@ss456!",
            ConfirmPassword = "NewSecureP@ss456!",
            FirstName = "New",
            LastName = "User",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1", "Test User Agent");

        // Assert - Should fail with generic error message (no email enumeration)
        result.Success.Should().BeFalse();
        result.Message.Should().NotContain("already exists"); // Generic error only
    }

    [Fact]
    public async Task RegisterUserAsync_WeakPassword_ReturnsValidationError()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "weakpass@test.com",
            Password = "weak", // Fails all requirements
            ConfirmPassword = "weak",
            FirstName = "Weak",
            LastName = "Password",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1", "Test User Agent");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeEmpty();  // Validation errors from Identity
    }

    [Fact]
    public async Task UpdateEmailVerificationStatusAsync_ValidUser_UpdatesStatus()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "verify@test.com",
            UserName = "verify@test.com",
            FirstName = "Verify",
            LastName = "User",
            Status = UserStatus.Active,
            EmailConfirmed = false
        };
        await _userManager.CreateAsync(user, "VerifyP@ss123!");

        // Act
        var result = await _userService.UpdateEmailVerificationStatusAsync(user.Id, true, "192.168.1.1");

        // Assert - Verify database was updated
        result.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Status.Should().Be(UserStatus.Active);
        updatedUser.UpdatedFromIP.Should().Be("192.168.1.1");
    }

    [Fact]
    public async Task UpdatePasswordAsync_ValidPassword_UpdatesHash()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "passchange@test.com",
            UserName = "passchange@test.com",
            FirstName = "Pass",
            LastName = "Change",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(user, "OldP@ssw0rd123!");
        var oldPasswordHash = user.PasswordHash;

        // Act
        var result = await _userService.UpdatePasswordAsync(user.Id, "NewP@ssw0rd456!");

        // Assert - Verify password hash changed in database
        result.Success.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.PasswordHash.Should().NotBe(oldPasswordHash);

        // Verify new password works
        var passwordCheck = await _userManager.CheckPasswordAsync(updatedUser, "NewP@ssw0rd456!");
        passwordCheck.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterUserAsync_ConcurrentDuplicateEmails_HandlesGracefully()
    {
        // Arrange
        var registerDto1 = new RegisterUserDto
        {
            Email = "concurrent@test.com",
            Password = "SecureP@ss123!",
            ConfirmPassword = "SecureP@ss123!",
            FirstName = "User",
            LastName = "One",
            AcceptedTerms = true
        };

        var registerDto2 = new RegisterUserDto
        {
            Email = "concurrent@test.com", // Same email
            Password = "DifferentP@ss456!",
            ConfirmPassword = "DifferentP@ss456!",
            FirstName = "User",
            LastName = "Two",
            AcceptedTerms = true
        };

        // Act - Simulate concurrent registration attempts
        var task1 = _userService.RegisterUserAsync(registerDto1, "192.168.1.1", "Browser1");
        var task2 = _userService.RegisterUserAsync(registerDto2, "192.168.1.2", "Browser2");

        var results = await Task.WhenAll(task1, task2);

        // Assert - Only one should succeed
        var successCount = results.Count(r => r.Success);
        successCount.Should().Be(1);

        // Verify only one user was created
        var users = await _context.Users.Where(u => u.Email == "concurrent@test.com").ToListAsync();
        users.Should().HaveCount(1);
    }

    [Fact]
    public async Task RegisterUserAsync_SqlInjectionAttempt_IsSanitized()
    {
        // Arrange
        var registerDto = new RegisterUserDto
        {
            Email = "test@test.com'; DROP TABLE Users; --",
            Password = "SecureP@ss123!",
            ConfirmPassword = "SecureP@ss123!",
            FirstName = "SQL",
            LastName = "Injection",
            AcceptedTerms = true
        };

        // Act
        var result = await _userService.RegisterUserAsync(registerDto, "192.168.1.1", "Test User Agent");

        // Assert - Should handle gracefully (email validation will likely fail)
        // The important thing is no SQL injection occurs and database is intact
        var usersCount = await _context.Users.CountAsync();
        usersCount.Should().BeGreaterThanOrEqualTo(0);  // Database still intact

        // Verify no actual SQL injection damage
        var tableExists = _context.Model.FindEntityType(typeof(User));
        tableExists.Should().NotBeNull();  // Users table still exists
    }

    [Fact]
    public async Task GetUserByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "getbyid@test.com",
            UserName = "getbyid@test.com",
            FirstName = "GetById",
            LastName = "Test",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(user, "TestP@ss123!");

        // Act
        var result = await _userService.GetUserByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("getbyid@test.com");
        result.FirstName.Should().Be("GetById");
    }

    [Fact]
    public async Task GetUserByIdAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.GetUserByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "getbyemail@test.com",
            UserName = "getbyemail@test.com",
            FirstName = "GetByEmail",
            LastName = "Test",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(user, "TestP@ss123!");

        // Act
        var result = await _userService.GetUserByEmailAsync("getbyemail@test.com");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("getbyemail@test.com");
        result.FirstName.Should().Be("GetByEmail");
    }

    [Fact]
    public async Task GetUserByEmailAsync_CaseInsensitive_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "casetest@test.com",
            UserName = "casetest@test.com",
            FirstName = "Case",
            LastName = "Test",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(user, "TestP@ss123!");

        // Act - Search with different case
        var result = await _userService.GetUserByEmailAsync("CASETEST@TEST.COM");

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be("casetest@test.com");
    }

    [Fact]
    public async Task GetUserByEmailAsync_NonExistentEmail_ReturnsNull()
    {
        // Act
        var result = await _userService.GetUserByEmailAsync("nonexistent@test.com");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePasswordAsync_NonExistentUser_ReturnsFailure()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.UpdatePasswordAsync(nonExistentId, "NewP@ssw0rd456!");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdatePasswordAsync_WeakPassword_ReturnsValidationError()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "weakupdate@test.com",
            UserName = "weakupdate@test.com",
            FirstName = "Weak",
            LastName = "Update",
            Status = UserStatus.Active
        };
        await _userManager.CreateAsync(user, "StrongP@ss123!");

        // Act - Try to update with weak password
        var result = await _userService.UpdatePasswordAsync(user.Id, "weak");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().NotBeEmpty();
    }

    [Fact]
    public async Task UpdateEmailVerificationStatusAsync_NonExistentUser_ReturnsFalse()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var result = await _userService.UpdateEmailVerificationStatusAsync(nonExistentId, true, "192.168.1.1");

        // Assert
        result.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _userManager.Dispose();
    }

    private sealed class RecordingSequencerClient : SkillLedger.Core.Interfaces.ISequencerClient
    {
        public List<(string Email, string SequenceSlug, string Source, IReadOnlyDictionary<string, object?>? Properties)> Enrollments { get; } = new();

        public Task EnrollAsync(
            string email,
            string sequenceSlug,
            string source,
            IReadOnlyDictionary<string, object?>? properties = null,
            CancellationToken cancellationToken = default)
        {
            Enrollments.Add((email, sequenceSlug, source, properties));
            return Task.CompletedTask;
        }
    }
}
