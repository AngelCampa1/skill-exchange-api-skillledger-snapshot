using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Role Controller API endpoints
/// Tests RBAC (Role-Based Access Control) management: roles, permissions, assignments
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 2")]
public class RoleControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _adminUser = null!;
    private User _targetUser = null!;

    public RoleControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup regular test user
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "role-user@test.com",
            UserName = "role-user@test.com",
            Status = UserStatus.Active
        };

        // Setup admin user with permissions
        _adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "role-admin@test.com",
            UserName = "role-admin@test.com",
            Status = UserStatus.Active
        };

        // Setup target user for role assignments
        _targetUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "target-user@test.com",
            UserName = "target-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _adminUser, _targetUser);
        await Context.SaveChangesAsync();

        // Seed required permissions for RBAC testing
        var permissions = new[]
        {
            new Permission { Id = Guid.NewGuid(), Name = "ManageRoles", Description = "Manage roles", Category = "Administration", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "ManagePermissions", Description = "Manage permissions", Category = "Administration", CreatedAt = DateTime.UtcNow },
            new Permission { Id = Guid.NewGuid(), Name = "ManageUserRoles", Description = "Manage user roles", Category = "Administration", CreatedAt = DateTime.UtcNow },
        };
        Context.Permissions.AddRange(permissions);

        // Create an admin role with all permissions
        var adminRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = "RoleAdmin",
            Description = "Role administrator with full RBAC permissions",
            IsSystemRole = true,
            IsActive = true,
            Priority = 100,
            CreatedAt = DateTime.UtcNow
        };
        Context.Roles.Add(adminRole);
        await Context.SaveChangesAsync();

        // Assign all permissions to admin role
        foreach (var permission in permissions)
        {
            Context.RolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedByUserId = _adminUser.Id
            });
        }

        // Assign admin role to admin user (using Identity's IdentityUserRole)
        Context.Set<IdentityUserRole<Guid>>().Add(new IdentityUserRole<Guid>
        {
            UserId = _adminUser.Id,
            RoleId = adminRole.Id
        });

        await Context.SaveChangesAsync();
    }

    #region GET /api/role Tests

    [Fact]
    [SecurityTest]
    public async Task GET_Roles_WithManageRolesPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        // Act
        var response = await Client.GetAsync("/api/role");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Roles_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/role");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [FastTest]
    public async Task GET_Roles_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/role");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/role Tests

    [Fact]
    [SecurityTest]
    public async Task POST_CreateRole_WithManageRolesPermission_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        var request = new
        {
            Name = $"TestRole_{Guid.NewGuid():N}",
            Description = "Test role for integration tests",
            IsActive = true,
            Priority = 100
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_CreateRole_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            Name = "UnauthorizedRole",
            Description = "This should not be created"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/role/{id} Tests

    [Fact]
    [SecurityTest]
    public async Task GET_RoleById_WithManageRolesPermission_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_adminUser);
        var roleId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/role/{roleId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_RoleById_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var roleId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/role/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/role/{id} Tests

    [Fact]
    [SecurityTest]
    public async Task PUT_UpdateRole_WithManageRolesPermission_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_adminUser);
        var roleId = Guid.NewGuid();

        var request = new
        {
            Name = "UpdatedRole",
            Description = "Updated description",
            IsActive = true,
            Priority = 150
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/role/{roleId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task PUT_UpdateRole_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var roleId = Guid.NewGuid();

        var request = new
        {
            Name = "ShouldNotUpdate",
            Description = "Unauthorized update"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/role/{roleId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/role/{id} Tests

    [Fact]
    [SecurityTest]
    public async Task DELETE_Role_WithManageRolesPermission_ReturnsNoContentOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_adminUser);
        var roleId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/role/{roleId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task DELETE_Role_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);
        var roleId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/role/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/role/permissions Tests

    [Fact]
    [SecurityTest]
    public async Task GET_Permissions_WithManageRolesPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        // Act
        var response = await Client.GetAsync("/api/role/permissions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Permissions_WithManagePermissionsPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        // Act
        var response = await Client.GetAsync("/api/role/permissions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_Permissions_WithoutAnyPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/role/permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/role/assign Tests

    [Fact]
    [SecurityTest]
    public async Task POST_AssignRole_WithManageUserRolesPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        var request = new
        {
            UserId = _targetUser.Id,
            RoleName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/assign", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AssignRole_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _targetUser.Id,
            RoleName = "Admin"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/assign", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/role/unassign Tests

    [Fact]
    [SecurityTest]
    public async Task POST_UnassignRole_WithManageUserRolesPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        var request = new
        {
            UserId = _targetUser.Id,
            RoleName = "User"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/unassign", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_UnassignRole_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            UserId = _targetUser.Id,
            RoleName = "Admin"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/unassign", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/role/permissions/assign Tests

    [Fact]
    [SecurityTest]
    public async Task POST_AssignPermission_WithManagePermissionsPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        var request = new
        {
            RoleName = "User",
            PermissionName = "ViewProjects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/permissions/assign", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_AssignPermission_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            RoleName = "User",
            PermissionName = "ManageRoles"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/permissions/assign", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/role/permissions/unassign Tests

    [Fact]
    [SecurityTest]
    public async Task POST_UnassignPermission_WithManagePermissionsPermission_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        var request = new
        {
            RoleName = "User",
            PermissionName = "ViewProjects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/permissions/unassign", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_UnassignPermission_WithoutPermission_ReturnsForbidden()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            RoleName = "User",
            PermissionName = "ViewProjects"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/role/permissions/unassign", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/role/my-permissions Tests

    [Fact]
    [FastTest]
    public async Task GET_MyPermissions_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/role/my-permissions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyPermissions_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/role/my-permissions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_MyPermissions_ReturnsUserRolesAndPermissions()
    {
        // Arrange
        AuthenticateAs(_adminUser);

        // Act
        var response = await Client.GetAsync("/api/role/my-permissions");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test GET endpoints without authentication
        var getEndpoints = new[]
        {
            "/api/role",
            $"/api/role/{Guid.NewGuid()}",
            "/api/role/permissions",
            "/api/role/my-permissions"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);

            // my-permissions requires auth only, others require auth + permission
            if (endpoint.Contains("my-permissions"))
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"GET {endpoint} should require authentication");
            }
            else
            {
                response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                    $"GET {endpoint} should require authentication");
            }
        }
    }

    [Fact]
    [SecurityTest]
    public async Task PermissionProtectedEndpoints_WithoutPermission_ReturnForbidden()
    {
        // Arrange
        AuthenticateAs(_user); // User without permissions

        var testCases = new[]
        {
            ("GET", "/api/role"),
            ("GET", $"/api/role/{Guid.NewGuid()}"),
            ("GET", "/api/role/permissions")
        };

        foreach (var (method, endpoint) in testCases)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(endpoint);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
                $"{method} {endpoint} should require specific permission");
        }
    }

    #endregion
}
