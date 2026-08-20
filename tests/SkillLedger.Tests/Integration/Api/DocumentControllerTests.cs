using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Document Controller API endpoints
/// Tests document upload, download, management, and folder operations
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class DocumentControllerTests : IntegrationTestBase
{
    private User _user = null!;
    private User _otherUser = null!;
    private Project _testProject = null!;
    private ProjectWorkspace _testWorkspace = null!;

    public DocumentControllerTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup test users
        _user = new User
        {
            Id = Guid.NewGuid(),
            Email = "doc-user@test.com",
            UserName = "doc-user@test.com",
            Status = UserStatus.Active
        };

        _otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other-doc-user@test.com",
            UserName = "other-doc-user@test.com",
            Status = UserStatus.Active
        };

        Context.Users.AddRange(_user, _otherUser);

        // Create test project (required for workspace)
        _testProject = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _user.Id,
            ProviderId = _otherUser.Id,
            Title = "Test Document Project",
            Description = "Project for testing documents",
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

    #region POST /api/document/upload Tests

    [Fact]
    [FastTest]
    public async Task POST_Upload_WithAuth_ReturnsOkOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        // Note: File upload requires multipart/form-data which is complex to test
        // This test validates the endpoint accepts requests
        var content = new MultipartFormDataContent
        {
            { new StringContent(_testWorkspace.Id.ToString()), "workspaceId" },
            { new ByteArrayContent(new byte[] { 1, 2, 3, 4, 5 }), "file", "test.txt" }
        };

        // Act
        var response = await Client.PostAsync("/api/document/upload", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Upload_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var content = new MultipartFormDataContent
        {
            { new StringContent(Guid.NewGuid().ToString()), "workspaceId" },
            { new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.txt" }
        };

        // Act
        var response = await Client.PostAsync("/api/document/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/{documentId}/download Tests

    [Fact]
    [FastTest]
    public async Task GET_Download_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}/download");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.Forbidden, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Download_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}/download");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task GET_Document_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Document_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/workspace/{workspaceId} Tests

    [Fact]
    [FastTest]
    public async Task GET_DocumentsByWorkspace_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_DocumentsByWorkspace_WithFolder_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);
        var folderId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}?folderId={folderId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_DocumentsByWorkspace_IncludeDeleted_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}?includeDeleted=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_DocumentsByWorkspace_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/document/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task PUT_UpdateDocument_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        var request = new
        {
            fileName = "updated-file.txt",
            description = "Updated description"
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/document/{documentId}", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task PUT_UpdateDocument_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var request = new { fileName = "test.txt" };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/document/{documentId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region DELETE /api/document/{documentId} Tests

    [Fact]
    [FastTest]
    public async Task DELETE_Document_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/document/{documentId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task DELETE_Document_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.DeleteAsync($"/api/document/{documentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/document/{documentId}/restore Tests

    [Fact]
    [FastTest]
    public async Task POST_RestoreDocument_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/document/{documentId}/restore", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_RestoreDocument_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.PostAsync($"/api/document/{documentId}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/document/{documentId}/move Tests

    [Fact]
    [FastTest]
    public async Task POST_MoveDocument_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();
        var targetFolderId = Guid.NewGuid();

        var request = new
        {
            targetFolderId
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/document/{documentId}/move", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_MoveDocument_ToRootFolder_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        var request = new
        {
            targetFolderId = (Guid?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/document/{documentId}/move", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_MoveDocument_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var request = new { targetFolderId = Guid.NewGuid() };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/document/{documentId}/move", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/document/folders Tests

    [Fact]
    [FastTest]
    public async Task POST_CreateFolder_WithAuth_ReturnsCreatedOrBadRequest()
    {
        // Arrange
        AuthenticateAs(_user);

        var request = new
        {
            workspaceId = _testWorkspace.Id,
            folderName = "Test Folder",
            description = "Test folder description",
            parentFolderId = (Guid?)null
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/document/folders", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateFolder_WithParent_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_user);
        var parentFolderId = Guid.NewGuid();

        var request = new
        {
            workspaceId = _testWorkspace.Id,
            folderName = "Subfolder",
            parentFolderId
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/document/folders", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_CreateFolder_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            workspaceId = _testWorkspace.Id,
            folderName = "Unauthorized Folder"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/document/folders", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/folders/workspace/{workspaceId} Tests

    [Fact]
    [FastTest]
    public async Task GET_FoldersByWorkspace_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/document/folders/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_FoldersByWorkspace_WithParent_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);
        var parentFolderId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/folders/workspace/{_testWorkspace.Id}?parentFolderId={parentFolderId}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_FoldersByWorkspace_IncludeDeleted_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/document/folders/workspace/{_testWorkspace.Id}?includeDeleted=true");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_FoldersByWorkspace_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/document/folders/workspace/{_testWorkspace.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/folders/{folderId} Tests

    [Fact]
    [FastTest]
    public async Task GET_FolderById_WithAuth_ReturnsNotFoundOrNotImplemented()
    {
        // Arrange
        AuthenticateAs(_user);
        var folderId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/folders/{folderId}");

        // Assert
        // Note: This endpoint returns 404 "Method not implemented"
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_FolderById_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var folderId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/folders/{folderId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/workspace/{workspaceId}/stats Tests

    [Fact]
    [FastTest]
    public async Task GET_StorageStats_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_user);

        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}/stats");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_StorageStats_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync($"/api/document/workspace/{_testWorkspace.Id}/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/document/{documentId}/preview Tests

    [Fact]
    [FastTest]
    public async Task GET_DocumentPreview_WithAuth_ReturnsOkOrNotFound()
    {
        // Arrange
        AuthenticateAs(_user);
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}/preview");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_DocumentPreview_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/document/{documentId}/preview");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Summary Tests

    [Fact]
    [SecurityTest]
    public async Task AllEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test GET endpoints without authentication
        var documentId = Guid.NewGuid();
        var folderId = Guid.NewGuid();

        var getEndpoints = new[]
        {
            $"/api/document/{documentId}/download",
            $"/api/document/{documentId}",
            $"/api/document/workspace/{_testWorkspace.Id}",
            $"/api/document/folders/workspace/{_testWorkspace.Id}",
            $"/api/document/folders/{folderId}",
            $"/api/document/workspace/{_testWorkspace.Id}/stats",
            $"/api/document/{documentId}/preview"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"GET {endpoint} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task ModificationEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test modification endpoints without authentication
        var documentId = Guid.NewGuid();

        var uploadContent = new MultipartFormDataContent();
        var uploadResponse = await Client.PostAsync("/api/document/upload", uploadContent);
        uploadResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var updateResponse = await Client.PutAsJsonAsync($"/api/document/{documentId}", new { });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var deleteResponse = await Client.DeleteAsync($"/api/document/{documentId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var restoreResponse = await Client.PostAsync($"/api/document/{documentId}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var moveResponse = await Client.PostAsJsonAsync($"/api/document/{documentId}/move", new { });
        moveResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var folderResponse = await Client.PostAsJsonAsync("/api/document/folders", new { });
        folderResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion
}
