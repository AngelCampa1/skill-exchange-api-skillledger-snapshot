using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
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
/// Integration tests for AuthorizationService - SECURITY CRITICAL.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real UserManager and RoleManager with in-memory stores
/// - Uses MockAuditLogService that writes to real database (internal service)
/// - Verifies actual database state and Identity store, not mock interactions
///
/// Max mocked external dependencies: 0 (Logger is OK)
/// </summary>
[IntegrationTest]
[SecurityTest]
public class AuthorizationServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<Role> _roleManager;
    private readonly MockAuditLogService _auditLogService;  // REAL internal service
    private readonly AuthorizationService _authorizationService;

    public AuthorizationServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"AuthorizationServiceTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup ASP.NET Identity with in-memory stores
        var userStore = new UserStore<User, Role, SkillLedgerDbContext, Guid>(_context);
        var roleStore = new RoleStore<Role, SkillLedgerDbContext, Guid>(_context);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();

        // Setup UserManager
        var userLogger = serviceProvider.GetRequiredService<ILogger<UserManager<User>>>();
        _userManager = new UserManager<User>(
            userStore,
            null,
            new PasswordHasher<User>(),
            new List<IUserValidator<User>> { new UserValidator<User>() },
            new List<IPasswordValidator<User>> { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            serviceProvider,
            userLogger
        );

        // Setup RoleManager
        var roleLogger = serviceProvider.GetRequiredService<ILogger<RoleManager<Role>>>();
        _roleManager = new RoleManager<Role>(
            roleStore,
            new List<IRoleValidator<Role>> { new RoleValidator<Role>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            roleLogger
        );

        // Setup services
        _auditLogService = new MockAuditLogService(_context);  // Writes to real DB!
        var mockLogger = new LoggerFactory().CreateLogger<AuthorizationService>();

        _authorizationService = new AuthorizationService(
            _context,
            _userManager,
            _roleManager,
            _auditLogService,
            mockLogger
        );
    }

    [Fact]
    public async Task HasPermissionAsync_UserHasPermission_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("permuser@test.com", "Perm", "User");
        var role = await CreateTestRoleAsync("TestRole");
        var permission = await CreateTestPermissionAsync("TEST_PERMISSION", "Testing");
        await AssignPermissionToRoleInDb(role.Id, permission.Id);
        await _userManager.AddToRoleAsync(user, role.Name!);

        // Act
        var result = await _authorizationService.HasPermissionAsync(user.Id, permission.Name);

        // Assert - Verify actual permission check
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_UserDoesNotHavePermission_ReturnsFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync("noperm@test.com", "No", "Perm");
        var role = await CreateTestRoleAsync("BasicRole");
        await _userManager.AddToRoleAsync(user, role.Name!);

        // Act
        var result = await _authorizationService.HasPermissionAsync(user.Id, "NONEXISTENT_PERMISSION");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPermissionAsync_UserNotFound_ReturnsFalse()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid();

        // Act
        var result = await _authorizationService.HasPermissionAsync(nonExistentUserId, "ANY_PERMISSION");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasAnyPermissionAsync_UserHasOneOfMultiplePermissions_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("any@test.com", "Any", "User");
        var role = await CreateTestRoleAsync("AnyRole");
        var permission1 = await CreateTestPermissionAsync("PERMISSION_1", "Testing");
        await AssignPermissionToRoleInDb(role.Id, permission1.Id);
        await _userManager.AddToRoleAsync(user, role.Name!);

        var permissionNames = new[] { "NONEXISTENT_PERMISSION", permission1.Name };

        // Act
        var result = await _authorizationService.HasAnyPermissionAsync(user.Id, permissionNames);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task HasAllPermissionsAsync_UserHasAllPermissions_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("all@test.com", "All", "User");
        var role = await CreateTestRoleAsync("AllRole");
        var permission1 = await CreateTestPermissionAsync("PERMISSION_A", "Testing");
        var permission2 = await CreateTestPermissionAsync("PERMISSION_B", "Testing");
        await AssignPermissionToRoleInDb(role.Id, permission1.Id);
        await AssignPermissionToRoleInDb(role.Id, permission2.Id);
        await _userManager.AddToRoleAsync(user, role.Name!);

        var permissionNames = new[] { permission1.Name, permission2.Name };

        // Act
        var result = await _authorizationService.HasAllPermissionsAsync(user.Id, permissionNames);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserPermissionsAsync_ValidUser_ReturnsPermissions()
    {
        // Arrange
        var user = await CreateTestUserAsync("getperms@test.com", "Get", "Perms");
        var role = await CreateTestRoleAsync("GetPermsRole");
        var permission1 = await CreateTestPermissionAsync("GET_PERM_1", "Testing");
        var permission2 = await CreateTestPermissionAsync("GET_PERM_2", "Testing");
        await AssignPermissionToRoleInDb(role.Id, permission1.Id);
        await AssignPermissionToRoleInDb(role.Id, permission2.Id);
        await _userManager.AddToRoleAsync(user, role.Name!);

        // Act
        var result = await _authorizationService.GetUserPermissionsAsync(user.Id);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(permission1.Name);
        result.Should().Contain(permission2.Name);
    }

    [Fact]
    public async Task AssignRoleAsync_ValidUserAndRole_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("assignrole@test.com", "Assign", "Role");
        var role = await CreateTestRoleAsync("AssignableRole");

        // Act
        var result = await _authorizationService.AssignRoleAsync(user.Id, role.Name!);

        // Assert - Verify database was updated
        result.Should().BeTrue();

        var userRoles = await _userManager.GetRolesAsync(user);
        userRoles.Should().Contain(role.Name);

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ROLE_ASSIGNED" && a.UserId == user.Id && a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain(role.Name!);
    }

    [Fact]
    public async Task AssignRoleAsync_AdminRoleByNonAdminManager_ReturnsFalse()
    {
        // Arrange
        var targetUser = await CreateTestUserAsync("target-admin-role@test.com", "Target", "Admin");
        var delegatedManager = await CreateTestUserAsync("delegated-role-manager@test.com", "Delegated", "Manager");
        await CreateTestRoleAsync(RoleNames.Admin, isSystemRole: true);

        // Act
        var result = await _authorizationService.AssignRoleAsync(
            targetUser.Id,
            RoleNames.Admin,
            delegatedManager.Id);

        // Assert
        result.Should().BeFalse("delegated role managers must not be able to grant Admin");

        var userRoles = await _userManager.GetRolesAsync(targetUser);
        userRoles.Should().NotContain(RoleNames.Admin);
    }

    [Fact]
    public async Task AssignRoleAsync_AdminRoleByAdmin_ReturnsTrue()
    {
        // Arrange
        var targetUser = await CreateTestUserAsync("target-admin-grant@test.com", "Target", "Grant");
        var adminUser = await CreateTestUserAsync("admin-role-grant@test.com", "Admin", "Grant");
        var adminRole = await CreateTestRoleAsync(RoleNames.Admin, isSystemRole: true);
        await _userManager.AddToRoleAsync(adminUser, adminRole.Name!);

        // Act
        var result = await _authorizationService.AssignRoleAsync(
            targetUser.Id,
            RoleNames.Admin,
            adminUser.Id);

        // Assert
        result.Should().BeTrue();

        var userRoles = await _userManager.GetRolesAsync(targetUser);
        userRoles.Should().Contain(RoleNames.Admin);
    }

    [Fact]
    public async Task RemoveRoleAsync_ValidUserAndRole_ReturnsTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync("removerole@test.com", "Remove", "Role");
        var role = await CreateTestRoleAsync("RemovableRole");
        await _userManager.AddToRoleAsync(user, role.Name!);

        // Act
        var result = await _authorizationService.RemoveRoleAsync(user.Id, role.Name!);

        // Assert - Verify database was updated
        result.Should().BeTrue();

        var userRoles = await _userManager.GetRolesAsync(user);
        userRoles.Should().NotContain(role.Name);

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ROLE_REMOVED" && a.UserId == user.Id && a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain(role.Name!);
    }

    [Fact]
    public async Task CreateRoleAsync_ValidRoleDto_CreatesRole()
    {
        // Arrange
        var permission = await CreateTestPermissionAsync("CREATE_ROLE_PERM", "Testing");
        var createRoleDto = new CreateRoleDto
        {
            Name = "NewTestRole",
            Description = "A new test role",
            Priority = 50,
            PermissionIds = new List<Guid> { permission.Id }
        };

        // Act
        var result = await _authorizationService.CreateRoleAsync(createRoleDto);

        // Assert - Verify role was created in database
        result.Should().NotBeNull();
        result!.Name.Should().Be(createRoleDto.Name);
        result.Description.Should().Be(createRoleDto.Description);
        result.IsSystemRole.Should().BeFalse();

        var roleInDb = await _roleManager.FindByNameAsync(createRoleDto.Name);
        roleInDb.Should().NotBeNull();
        roleInDb!.Description.Should().Be(createRoleDto.Description);

        // Verify permissions assigned
        var rolePermissions = await _context.RolePermissions
            .Where(rp => rp.RoleId == roleInDb.Id)
            .ToListAsync();
        rolePermissions.Should().HaveCount(1);

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ROLE_CREATED" && a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain(createRoleDto.Name);
    }

    [Fact]
    public async Task CreateRoleAsync_PrivilegedPermissionByNonAdmin_ReturnsNull()
    {
        // Arrange
        var delegatedManager = await CreateTestUserAsync("delegated-create-role@test.com", "Delegated", "Create");
        var privilegedPermission = await CreateTestPermissionAsync(PermissionNames.ManageRoles, "Administration");
        var createRoleDto = new CreateRoleDto
        {
            Name = "EscalatingRole",
            Description = "Should not be created by delegated managers",
            Priority = 50,
            PermissionIds = new List<Guid> { privilegedPermission.Id }
        };

        // Act
        var result = await _authorizationService.CreateRoleAsync(createRoleDto, delegatedManager.Id);

        // Assert
        result.Should().BeNull();
        (await _roleManager.FindByNameAsync(createRoleDto.Name)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteRoleAsync_NonSystemRoleWithNoUsers_ReturnsTrue()
    {
        // Arrange
        var role = await CreateTestRoleAsync("DeletableRole", isSystemRole: false);

        // Act
        var result = await _authorizationService.DeleteRoleAsync(role.Id);

        // Assert - Verify role was deleted from database
        result.Should().BeTrue();

        var roleInDb = await _roleManager.FindByIdAsync(role.Id.ToString());
        roleInDb.Should().BeNull();

        // Verify audit log in database
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "ROLE_DELETED" && a.Success);
        auditLog.Should().NotBeNull();
        auditLog!.Details.Should().Contain(role.Name!);
    }

    [Fact]
    public async Task DeleteRoleAsync_SystemRole_ReturnsFalse()
    {
        // Arrange
        var systemRole = await CreateTestRoleAsync("SystemRole", isSystemRole: true);

        // Act
        var result = await _authorizationService.DeleteRoleAsync(systemRole.Id);

        // Assert - Verify role was NOT deleted
        result.Should().BeFalse();

        var roleInDb = await _roleManager.FindByIdAsync(systemRole.Id.ToString());
        roleInDb.Should().NotBeNull();  // Still exists
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_ValidRoleAndPermission_ReturnsTrue()
    {
        // Arrange
        var role = await CreateTestRoleAsync("PermAssignRole");
        var permission = await CreateTestPermissionAsync("NEW_PERMISSION", "Testing");

        // Act
        var result = await _authorizationService.AssignPermissionToRoleAsync(role.Name!, permission.Name);

        // Assert - Verify assignment in database
        result.Should().BeTrue();

        var assignment = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        assignment.Should().NotBeNull();
        assignment!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task AssignPermissionToRoleAsync_PrivilegedPermissionByNonAdmin_ReturnsFalse()
    {
        // Arrange
        var delegatedManager = await CreateTestUserAsync("delegated-permission-manager@test.com", "Delegated", "Permission");
        var role = await CreateTestRoleAsync("DelegatedManagedRole");
        var permission = await CreateTestPermissionAsync(PermissionNames.ManageRoles, "Administration");

        // Act
        var result = await _authorizationService.AssignPermissionToRoleAsync(
            role.Name!,
            permission.Name,
            delegatedManager.Id);

        // Assert
        result.Should().BeFalse("delegated permission managers must not mint admin-equivalent power");

        var assignment = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == role.Id && rp.PermissionId == permission.Id);
        assignment.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPermissionsByCategoryAsync_HasPermissions_ReturnsGroupedPermissions()
    {
        // Arrange
        await CreateTestPermissionAsync("CATEGORY_PERM_1", "CategoryA");
        await CreateTestPermissionAsync("CATEGORY_PERM_2", "CategoryA");
        await CreateTestPermissionAsync("CATEGORY_PERM_3", "CategoryB");

        // Act
        var result = await _authorizationService.GetAllPermissionsByCategoryAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().ContainKey("CategoryA");
        result.Should().ContainKey("CategoryB");
        result["CategoryA"].Should().HaveCount(2);
        result["CategoryB"].Should().HaveCount(1);
    }

    [Fact]
    public async Task InitializeSystemRolesAndPermissionsAsync_CreatesSystemData()
    {
        // Arrange - Clear any existing data
        _context.RolePermissions.RemoveRange(_context.RolePermissions);
        _context.Permissions.RemoveRange(_context.Permissions);
        await _context.SaveChangesAsync();

        // Act
        await _authorizationService.InitializeSystemRolesAndPermissionsAsync();

        // Assert - Verify permissions were created in database
        var permissionCount = await _context.Permissions.CountAsync();
        permissionCount.Should().BeGreaterThanOrEqualTo(PermissionNames.All.Length);

        // Verify all roles were created
        foreach (var roleName in RoleNames.All)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            role.Should().NotBeNull($"Role {roleName} should be created");
        }

        // Verify system roles have IsSystemRole = true
        foreach (var roleName in RoleNames.SystemRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            role.Should().NotBeNull($"System role {roleName} should be created");
            role!.IsSystemRole.Should().BeTrue($"{roleName} is a system role");
        }

        // Verify non-system roles have IsSystemRole = false
        var nonSystemRoles = RoleNames.All.Except(RoleNames.SystemRoles);
        foreach (var roleName in nonSystemRoles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            role.Should().NotBeNull($"Role {roleName} should be created");
            role!.IsSystemRole.Should().BeFalse($"{roleName} is not a system role");
        }
    }

    #region Helper Methods

    private async Task<User> CreateTestUserAsync(string email, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = email.ToUpperInvariant(),
            FirstName = firstName,
            LastName = lastName,
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    private async Task<Role> CreateTestRoleAsync(string roleName, bool isSystemRole = false)
    {
        var role = new Role(roleName)
        {
            Id = Guid.NewGuid(),
            Description = $"Test role: {roleName}",
            IsSystemRole = isSystemRole,
            Priority = 100
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            throw new Exception($"Failed to create test role: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return role;
    }

    private async Task<Permission> CreateTestPermissionAsync(string permissionName, string category)
    {
        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Name = permissionName,
            Description = $"Test permission: {permissionName}",
            Category = category,
            IsActive = true
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync();

        return permission;
    }

    private async Task AssignPermissionToRoleInDb(Guid roleId, Guid permissionId)
    {
        var rolePermission = new RolePermission
        {
            RoleId = roleId,
            PermissionId = permissionId,
            IsActive = true
        };

        _context.RolePermissions.Add(rolePermission);
        await _context.SaveChangesAsync();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _userManager.Dispose();
        _roleManager.Dispose();
    }
}
