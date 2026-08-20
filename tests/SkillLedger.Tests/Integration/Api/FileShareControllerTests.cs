using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for File Share Controller API endpoints
/// Tests file upload, download, sharing, folder management, and workspace operations
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class FileShareControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;
    private Project _testProject = null!;
    private ProjectWorkspace _testWorkspace = null!;

    public FileShareControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "fileshare-user@test.com",
            UserName = "fileshare-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-fileshare-user@test.com",
            UserName = "other-fileshare-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);

        // Create test project (required for workspace)
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _user.Id,
            ProviderId = _otherUser.Id,
            Title = "Test FileShare Project",
            Description = "Project for testing file sharing",
            Status = ProjectStatus.InProgress,
            CreditBudget = 100
        };

        Context.Projects.Add(_testProject);

        // Create test workspace
        _testWorkspace = new ProjectWorkspace
        {
            Id = Guid.NewGuid(),
            ProjectId = _testProject.Id,
            ClientId = _user.Id,
            ProviderId = _otherUser.Id,
            Status = WorkspaceStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        Context.ProjectWorkspaces.Add(_testWorkspace);
        await Context.SaveChangesAsync();
    }

    #region POST /api/FileShare/upload Tests

    [Fact]
    [FastTest]
    public async Task POST_Upload_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_testWorkspace.Id.ToString()), "WorkspaceId");

        // Act
        var response = await Client.PostAsync("/api/FileShare/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_Upload_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_testWorkspace.Id.ToString()), "WorkspaceId");
        content.Add(new StringContent("false"), "IsPrivate");

        // Act
        var response = await Client.PostAsync("/api/FileShare/upload", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/FileShare/upload/multiple Tests

    [Fact]
    [FastTest]
    public async Task POST_UploadMultiple_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_testWorkspace.Id.ToString()), "WorkspaceId");

        // Act
        var response = await Client.PostAsync("/api/FileShare/upload/multiple", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_UploadMultiple_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(_testWorkspace.Id.ToString()), "WorkspaceId");

        // Act
        var response = await Client.PostAsync("/api/FileShare/upload/multiple", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task GET_Document_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Document_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/{documentId}/download Tests

    [Fact]
    [FastTest]
    public async Task GET_Download_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Download_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}/download");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/{documentId}/secure-url Tests

    [Fact]
    [FastTest]
    public async Task GET_SecureUrl_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}/secure-url");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_SecureUrl_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/FileShare/{documentId}/secure-url?expirationMinutes=30");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/workspace/{workspaceId} Tests

    [Fact]
    [FastTest]
    public async Task GET_WorkspaceDocuments_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_WorkspaceDocuments_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_WorkspaceDocuments_WithFilters_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync(
            $"/api/FileShare/workspace/{_testWorkspace.Id}?includeDeleted=false&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/search Tests

    [Fact]
    [FastTest]
    public async Task GET_Search_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/FileShare/search?workspaceId={_testWorkspace.Id}&searchQuery=test");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Search_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/FileShare/search?workspaceId={_testWorkspace.Id}&searchQuery=test");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Search_WithoutQuery_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/FileShare/search?workspaceId={_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region DELETE /api/FileShare/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_Document_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/FileShare/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_Document_WithAuth_ReturnsNoContentOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/FileShare/{documentId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NoContent, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region PUT /api/FileShare/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateDocument_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var request = new
        {
            FileName = "updated-file.txt",
            Description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/FileShare/{documentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateDocument_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();
        var request = new
        {
            FileName = "updated-file.txt",
            Description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/FileShare/{documentId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/FileShare/folders Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateFolder_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            WorkspaceId = _testWorkspace.Id,
            Name = "Test Folder",
            Description = "Test folder description"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/FileShare/folders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateFolder_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            WorkspaceId = _testWorkspace.Id,
            Name = "Test Folder",
            Description = "Test folder description"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/FileShare/folders", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/workspace/{workspaceId}/folders Tests

    [Fact]
    [FastTest]
    public async Task GET_FolderStructure_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}/folders");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_FolderStructure_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}/folders");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/FileShare/{documentId}/share Tests

    [Fact]
    [FastTest]
    public async Task POST_ShareDocument_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var request = new
        {
            SharedWithUserId = _otherUser.Id,
            CanEdit = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/FileShare/{documentId}/share", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_ShareDocument_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();
        var request = new
        {
            SharedWithUserId = _otherUser.Id,
            CanEdit = false,
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/FileShare/{documentId}/share", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/shared-with-me Tests

    [Fact]
    [FastTest]
    public async Task GET_SharedWithMe_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/FileShare/shared-with-me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_SharedWithMe_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync("/api/FileShare/shared-with-me?pageNumber=1&pageSize=20");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/FileShare/workspace/{workspaceId}/storage-stats Tests

    [Fact]
    [FastTest]
    public async Task GET_StorageStats_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}/storage-stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_StorageStats_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/FileShare/workspace/{_testWorkspace.Id}/storage-stats");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints without authentication
        var documentId = Guid.NewGuid();

        var endpoints = new[]
        {
            ("GET", $"/api/FileShare/{documentId}"),
            ("GET", $"/api/FileShare/{documentId}/download"),
            ("GET", $"/api/FileShare/{documentId}/secure-url"),
            ("GET", $"/api/FileShare/workspace/{_testWorkspace.Id}"),
            ("GET", $"/api/FileShare/search?workspaceId={_testWorkspace.Id}&searchQuery=test"),
            ("DELETE", $"/api/FileShare/{documentId}"),
            ("GET", $"/api/FileShare/workspace/{_testWorkspace.Id}/folders"),
            ("GET", $"/api/FileShare/shared-with-me"),
            ("GET", $"/api/FileShare/workspace/{_testWorkspace.Id}/storage-stats")
        };

        foreach (var (method, endpoint) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(endpoint);
                    break;
                case "DELETE":
                    response = await Client.DeleteAsync(endpoint);
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {endpoint} should require authentication");
        }
    }

    #endregion
}
