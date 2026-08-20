using Microsoft.AspNetCore.Mvc.Testing;
using SkillLedger.Core.Constants;
using SkillLedger.Tests.Fixtures;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using Xunit;

namespace SkillLedger.Tests.Integration;

[Collection("Integration Other")]
[IntegrationTest]
[SecurityTest]
public class SimpleRoleManagementTests : IntegrationTestBase
{
    public SimpleRoleManagementTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [SecurityTest]
    public async Task GetRoles_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await Client.GetAsync("/api/role");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [SecurityTest]
    public async Task GetPermissions_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await Client.GetAsync("/api/role/permissions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [SecurityTest]
    public async Task CreateRole_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await Client.PostAsync("/api/role", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [SecurityTest]
    public async Task GetMyPermissions_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await Client.GetAsync("/api/role/my-permissions");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    [SecurityTest]
    public async Task AssignRole_WithoutAuthentication_Returns401()
    {
        // Act
        var response = await Client.PostAsync("/api/role/assign", null);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}