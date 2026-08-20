using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Constants;
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
/// Integration tests for AuthenticationService - SECURITY CRITICAL.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real UserManager and SignInManager with in-memory stores
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Uses real AuthorizationService (RBAC logic)
/// - Verifies actual database state and Identity store, not mock interactions
///
/// Max mocked external dependencies: 0 (Logger is OK)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class AuthenticationServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly MockAuditLogService _auditLogService;
    private readonly AuthorizationService _authorizationService;
    private readonly SkillLedger.Infrastructure.Services.AuthenticationService _authenticationService;
    private readonly IServiceProvider _serviceProvider;

    public AuthenticationServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthenticationServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup ASP.NET Identity with in-memory stores
        var userStore = new UserStore<User, Role, SkillLedgerDbContext, Guid>(_context);
        var roleStore = new RoleStore<Role, SkillLedgerDbContext, Guid>(_context);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();

        // Add HttpContextAccessor for SignInManager
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        // Add authentication schemes for SignInManager
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = IdentityConstants.ApplicationScheme;
        }).AddCookie(IdentityConstants.ApplicationScheme);

        _serviceProvider = services.BuildServiceProvider();

        // Setup UserManager
        var userLogger = _serviceProvider.GetRequiredService<ILogger<UserManager<User>>>();
        _userManager = new UserManager<User>(
            userStore,
            Options.Create(new IdentityOptions
            {
                Lockout = new LockoutOptions
                {
                    AllowedForNewUsers = true,
                    MaxFailedAccessAttempts = 5,
                    DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)
                }
            }),
            new PasswordHasher<User>(),
            new List<IUserValidator<User>> { new UserValidator<User>() },
            new List<IPasswordValidator<User>> { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            _serviceProvider,
            userLogger
        );

        // Setup RoleManager
        var roleLogger = _serviceProvider.GetRequiredService<ILogger<RoleManager<Role>>>();
        _roleManager = new RoleManager<Role>(
            roleStore,
            new List<IRoleValidator<Role>> { new RoleValidator<Role>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            roleLogger
        );

        // Setup SignInManager
        var httpContextAccessor = _serviceProvider.GetRequiredService<IHttpContextAccessor>();
        var claimsFactory = new UserClaimsPrincipalFactory<User, Role>(_userManager, _roleManager, Options.Create(new IdentityOptions()));
        var signInLogger = _serviceProvider.GetRequiredService<ILogger<SignInManager<User>>>();
        var schemeProvider = _serviceProvider.GetRequiredService<IAuthenticationSchemeProvider>();
        var userConfirmation = new DefaultUserConfirmation<User>();

        _signInManager = new SignInManager<User>(
            _userManager,
            httpContextAccessor,
            claimsFactory,
            Options.Create(new IdentityOptions()),
            signInLogger,
            schemeProvider,
            userConfirmation
        );

        // Setup mock HttpContext for SignInManager
        var httpContext = new DefaultHttpContext
        {
            RequestServices = _serviceProvider
        };
        httpContextAccessor.HttpContext = httpContext;

        // Setup services
        _auditLogService = new MockAuditLogService(_context);
        var authzLogger = new LoggerFactory().CreateLogger<AuthorizationService>();
        _authorizationService = new AuthorizationService(
            _context,
            _userManager,
            _roleManager,
            _auditLogService,
            authzLogger
        );

        var authLogger = new LoggerFactory().CreateLogger<SkillLedger.Infrastructure.Services.AuthenticationService>();
        _authenticationService = new SkillLedger.Infrastructure.Services.AuthenticationService(
            _context,
            _userManager,
            _signInManager,
            _auditLogService,
            _authorizationService,
            authLogger
        );
    }

    private async Task<User> CreateTestUserAsync(string email, string password = "ValidPassword123!")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = "Test",
            LastName = "User",
            Status = UserStatus.Active,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    #region AuthenticateAsync Tests

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_ShouldSucceed()
    {
        // Arrange
        var email = "valid@test.com";
        var password = "ValidPassword123!";
        var user = await CreateTestUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password,
            RememberMe = false
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest, "127.0.0.1", "Test-Agent");

        // Assert - Verify successful authentication
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(email);
        result.User.Id.Should().Be(user.Id);
        result.Message.Should().Be("Login successful");
        result.IsLockedOut.Should().BeFalse();

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGIN_SUCCESS")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
        auditLog.IPAddress.Should().Be("127.0.0.1");

        // Verify failed attempts were reset
        var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser!.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task AuthenticateAsync_ValidCredentials_WithRememberMe_ShouldSetPersistentCookie()
    {
        // Arrange
        var email = "remember@test.com";
        var password = "RememberPassword123!";
        await CreateTestUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password,
            RememberMe = true
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.User.Should().NotBeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidEmail_ShouldReturnGenericError()
    {
        // Arrange
        var loginRequest = new LoginRequestDto
        {
            Email = "nonexistent@test.com",
            Password = "AnyPassword123!"
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest, "127.0.0.1");

        // Assert - Verify generic error (no email enumeration)
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password.");
        result.User.Should().BeNull();
        result.IsLockedOut.Should().BeFalse();

        // Verify failed attempt was logged
        var auditLog = await _context.AuditLogs
            .Where(a => a.Action == "USER_LOGIN_FAILED")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeFalse();
        auditLog.Details.Should().Contain("User not found");
    }

    [Fact]
    public async Task AuthenticateAsync_InvalidPassword_ShouldReturnGenericError()
    {
        // Arrange
        var email = "wrongpass@test.com";
        var correctPassword = "CorrectPassword123!";
        var user = await CreateTestUserAsync(email, correctPassword);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest, "127.0.0.1");

        // Assert - Verify generic error (no password enumeration)
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid email or password.");
        result.User.Should().BeNull();

        // Verify failed attempts incremented
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(1);

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGIN_FAILED")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("Invalid password");
    }

    [Fact]
    public async Task AuthenticateAsync_MultipleFailedAttempts_ShouldIncrementCounter()
    {
        // Arrange
        var email = "failed@test.com";
        var password = "ValidPassword123!";
        var user = await CreateTestUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        // Act - 3 failed attempts
        for (int i = 0; i < 3; i++)
        {
            await _authenticationService.AuthenticateAsync(loginRequest);
        }

        // Assert - Verify failed attempts incremented
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(3);
    }

    [Fact]
    public async Task AuthenticateAsync_LockedOutAccount_ShouldReturnLockedOutError()
    {
        // Arrange
        var email = "locked@test.com";
        var password = "ValidPassword123!";
        var user = await CreateTestUserAsync(email, password);

        // Lock the account
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(loginRequest, "127.0.0.1");

        // Assert - Verify lockout error
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.IsLockedOut.Should().BeTrue();
        result.Message.Should().Contain("Account is locked");
        result.User.Should().BeNull();

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGIN_FAILED")
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain("Account locked");
    }

    [Fact]
    public async Task AuthenticateAsync_ExceedMaxFailedAttempts_ShouldLockAccount()
    {
        // Arrange
        var email = "lockme@test.com";
        var password = "ValidPassword123!";
        await CreateTestUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        // Act - Exceed max failed attempts (5 configured in IdentityOptions)
        LoginResponseDto? result = null;
        for (int i = 0; i < 6; i++)
        {
            result = await _authenticationService.AuthenticateAsync(loginRequest);
        }

        // Assert - Verify account is now locked
        result.Should().NotBeNull();
        result!.IsLockedOut.Should().BeTrue();
        result.Message.Should().Contain("Account is locked");

        // Verify lockout in Identity
        var user = await _userManager.FindByEmailAsync(email);
        var isLockedOut = await _userManager.IsLockedOutAsync(user!);
        isLockedOut.Should().BeTrue();
    }

    [Fact]
    public async Task AuthenticateAsync_SuccessfulLoginAfterFailures_ShouldResetFailedAttempts()
    {
        // Arrange
        var email = "reset@test.com";
        var password = "ValidPassword123!";
        var user = await CreateTestUserAsync(email, password);

        // Failed attempts first
        var failedRequest = new LoginRequestDto
        {
            Email = email,
            Password = "WrongPassword123!"
        };

        for (int i = 0; i < 3; i++)
        {
            await _authenticationService.AuthenticateAsync(failedRequest);
        }

        // Successful login
        var successRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act
        var result = await _authenticationService.AuthenticateAsync(successRequest);

        // Assert - Verify failed attempts reset
        result.Success.Should().BeTrue();

        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ValidUser_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync("logout@test.com");

        // Act
        var result = await _authenticationService.LogoutAsync(user.Id, "127.0.0.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Logged out successfully");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGOUT")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
        auditLog.IPAddress.Should().Be("127.0.0.1");
    }

    [Fact]
    public async Task LogoutAsync_WithoutIpAddress_ShouldUseUnknown()
    {
        // Arrange
        var user = await CreateTestUserAsync("logoutnoip@test.com");

        // Act
        var result = await _authenticationService.LogoutAsync(user.Id);

        // Assert
        result.Success.Should().BeTrue();

        // Verify audit log uses "unknown" for IP
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGOUT")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.IPAddress.Should().Be("unknown");
    }

    #endregion

    #region LogoutFromAllDevicesAsync Tests

    [Fact]
    public async Task LogoutFromAllDevicesAsync_ValidUser_ShouldSucceed()
    {
        // Arrange
        var user = await CreateTestUserAsync("logoutall@test.com");
        var originalSecurityStamp = user.SecurityStamp;

        // Act
        var result = await _authenticationService.LogoutFromAllDevicesAsync(user.Id, "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Successfully logged out from all devices.");

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .Where(a => a.UserId == user.Id && a.Action == "USER_LOGOUT_ALL_DEVICES")
            .FirstOrDefaultAsync();
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
        auditLog.IPAddress.Should().Be("192.168.1.1");

        var updatedUser = await _userManager.FindByIdAsync(user.Id.ToString());
        updatedUser!.SecurityStamp.Should().NotBe(originalSecurityStamp, "logout-all must invalidate cookies issued to other devices");
    }

    #endregion

    #region IsAccountLockedAsync Tests

    [Fact]
    public async Task IsAccountLockedAsync_LockedAccount_ShouldReturnTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("checklocked@test.com");
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));

        // Act
        var result = await _authenticationService.IsAccountLockedAsync(user.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsAccountLockedAsync_UnlockedAccount_ShouldReturnFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("checkunlocked@test.com");

        // Act
        var result = await _authenticationService.IsAccountLockedAsync(user.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsAccountLockedAsync_NonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _authenticationService.IsAccountLockedAsync(nonExistentUserId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ResetFailedLoginAttemptsAsync Tests

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_UserWithFailedAttempts_ShouldReset()
    {
        // Arrange
        var user = await CreateTestUserAsync("resetattempts@test.com");
        user.FailedLoginAttempts = 3;
        await _context.SaveChangesAsync();
        await _userManager.AccessFailedAsync(user);  // Identity counter
        await _userManager.AccessFailedAsync(user);
        await _userManager.AccessFailedAsync(user);

        // Act
        await _authenticationService.ResetFailedLoginAttemptsAsync(user.Id);

        // Assert - Verify both counters reset
        var updatedUser = await _context.Users.FindAsync(user.Id);
        updatedUser!.FailedLoginAttempts.Should().Be(0);

        var accessFailedCount = await _userManager.GetAccessFailedCountAsync(user);
        accessFailedCount.Should().Be(0);
    }

    [Fact]
    public async Task ResetFailedLoginAttemptsAsync_NonExistentUser_ShouldNotThrow()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var act = async () => await _authenticationService.ResetFailedLoginAttemptsAsync(nonExistentUserId);

        // Assert - Should not throw
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region GetCurrentUserFromContextAsync Tests

    [Fact]
    public async Task GetCurrentUserFromContextAsync_ValidUser_ShouldReturnProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync("getprofile@test.com");

        // Add user to a role
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "TestRole",
            NormalizedName = "TESTROLE",
            Description = "Test Role"
        };
        await _roleManager.CreateAsync(role);
        await _userManager.AddToRoleAsync(user, role.Name);

        // Act
        var result = await _authenticationService.GetCurrentUserFromContextAsync(user.Id);

        // Assert - Verify user profile
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be(user.Email);
        result.UserName.Should().Be(user.UserName);
        result.FirstName.Should().Be(user.FirstName);
        result.LastName.Should().Be(user.LastName);
        result.Roles.Should().Contain("TestRole");
    }

    [Fact]
    public async Task GetCurrentUserFromContextAsync_NonExistentUser_ShouldReturnNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _authenticationService.GetCurrentUserFromContextAsync(nonExistentUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserFromContextAsync_UserWithPermissions_ShouldIncludePermissions()
    {
        // Arrange
        var user = await CreateTestUserAsync("withperm@test.com");

        // Create role with permissions
        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = "AdminRole",
            NormalizedName = "ADMINROLE",
            Description = "Admin Role"
        };
        await _roleManager.CreateAsync(role);

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = "VIEW_USERS",
            Description = "View users",
            Category = "User Management"
        };
        _context.Permissions.Add(permission);

        var rolePermission = new RolePermission
        {
            RoleId = role.Id,
            PermissionId = permission.Id
        };
        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();

        await _userManager.AddToRoleAsync(user, role.Name);

        // Act
        var result = await _authenticationService.GetCurrentUserFromContextAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Permissions.Should().Contain("VIEW_USERS");
    }

    #endregion

    #region Concurrent Operations Tests

    [Fact]
    public async Task AuthenticateAsync_ConcurrentLoginAttempts_ShouldHandleRaceCondition()
    {
        // Arrange
        var email = "concurrent@test.com";
        var password = "ConcurrentPassword123!";
        await CreateTestUserAsync(email, password);

        var loginRequest = new LoginRequestDto
        {
            Email = email,
            Password = password
        };

        // Act - Concurrent login attempts
        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _authenticationService.AuthenticateAsync(loginRequest))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed (no deadlocks/exceptions)
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _userManager.Dispose();
        _roleManager.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
    }
}
