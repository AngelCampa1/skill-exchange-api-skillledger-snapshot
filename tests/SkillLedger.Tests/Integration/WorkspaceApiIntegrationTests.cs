using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SkillLedger.Tests.Integration;

[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class WorkspaceApiIntegrationTests : IntegrationTestBase
{
    private User _testUser = null!;
    private Project _testProject = null!;
    private User _testProvider = null!;

    public WorkspaceApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test user
        _testUser = await CreateTestUserAsync("client@test.com", "TestPass123!");
        _testProvider = await CreateTestUserAsync("provider@test.com", "TestPass123!");

        // Setup test project
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testUser.Id,
            Title = "Test Project for Workspace",
            Description = "Test project for workspace integration testing",
            Status = ProjectStatus.Published,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Context.Projects.Add(_testProject);
        await Context.SaveChangesAsync();
    }

    [Fact]
    [FastTest]
    public void Test_ProjectWorkspace_Can_Be_Instantiated()
    {
        // Arrange & Act
        var workspace = new ProjectWorkspace
        {
            ProjectId = _testProject.Id,
            ClientId = _testUser.Id,
            ProviderId = _testProvider.Id
        };

        // Assert
        Assert.NotEqual(Guid.Empty, workspace.Id);
        Assert.Equal(WorkspaceStatus.Active, workspace.Status);
        Assert.NotNull(workspace.WorkspaceKey);
        Assert.NotEmpty(workspace.WorkspaceKey);
    }

    [Fact]
    [FastTest]
    public void Test_ProjectWorkspace_ArchiveWorkspace_Changes_Status()
    {
        // Arrange
        var workspace = new ProjectWorkspace
        {
            ProjectId = _testProject.Id,
            ClientId = _testUser.Id,
            ProviderId = _testProvider.Id
        };

        // Act
        workspace.ArchiveWorkspace();

        // Assert
        Assert.Equal(WorkspaceStatus.Archived, workspace.Status);
        Assert.NotNull(workspace.ArchivedAt);
    }

    [Fact]
    [SecurityTest]
    public void Test_ProjectWorkspace_IsAccessibleBy_Returns_Correct_Access()
    {
        // Arrange
        var workspace = new ProjectWorkspace
        {
            ProjectId = _testProject.Id,
            ClientId = _testUser.Id,
            ProviderId = _testProvider.Id
        };

        // Act & Assert
        Assert.True(workspace.IsAccessibleBy(_testUser.Id));
        Assert.True(workspace.IsAccessibleBy(_testProvider.Id));
        Assert.False(workspace.IsAccessibleBy(Guid.NewGuid()));
    }
}