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

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[CoreTest]
[Collection("Integration Financial")]
public class ProjectServiceTests : IntegrationTestBase
{
    private readonly IProjectService _service;
    private User _testClient = null!;
    private Skill _testSkill1 = null!;
    private Skill _testSkill2 = null!;

    public ProjectServiceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _service = ServiceScope.ServiceProvider.GetRequiredService<IProjectService>();
    }

    protected override async Task OnInitializeAsync()
    {
        // CRITICAL FIX: Call base initialization first to setup database
        await base.OnInitializeAsync();

        // Setup test data using async initialization
        // This avoids blocking calls in the constructor and ensures proper database setup
        _testClient = new User
        {
            Id = Guid.NewGuid(),
            Email = "client@example.com",
            UserName = "client@example.com",
            NormalizedEmail = "CLIENT@EXAMPLE.COM",
            NormalizedUserName = "CLIENT@EXAMPLE.COM",
            EmailConfirmed = true,
            Status = UserStatus.Active,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow
        };

        _testSkill1 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "C# Programming",
            Description = "Programming in C#",
            Category = "Programming",
            IsActive = true,
            IsSystemManaged = true
        };

        _testSkill2 = new Skill
        {
            Id = Guid.NewGuid(),
            Name = "React Development",
            Description = "Frontend development with React",
            Category = "Frontend",
            IsActive = true,
            IsSystemManaged = true
        };

        // Add test data to context
        Context.Users.Add(_testClient);
        Context.Skills.AddRange(_testSkill1, _testSkill2);
        await Context.SaveChangesAsync();
    }

    #region CreateProjectAsync Tests

    [Fact]
    public async Task CreateProjectAsync_ValidData_CreatesProject()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Title = "Test Project",
            Description = "This is a test project for unit testing",
            CreditBudget = 500,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Deliverables = new List<CreateProjectDeliverableDto>
            {
                new CreateProjectDeliverableDto
                {
                    Description = "First deliverable",
                    OrderIndex = 1,
                    IsRequired = true
                }
            },
            RequiredSkills = new List<CreateProjectSkillDto>
            {
                new CreateProjectSkillDto
                {
                    SkillId = _testSkill1.Id,
                    ProficiencyRequired = 3,
                    Weight = 4
                }
            }
        };

        // Act
        var result = await _service.CreateProjectAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal(createDto.Title, result.Project.Title);
        Assert.Equal(createDto.Description, result.Project.Description);
        Assert.Equal(createDto.CreditBudget, result.Project.CreditBudget);
        Assert.Equal("Draft", result.Project.Status);
        Assert.Single(result.Project.Deliverables);
        Assert.Single(result.Project.RequiredSkills);

        // Verify database
        var project = await Context.Projects.Include(p => p.Deliverables).Include(p => p.ProjectSkills)
            .FirstOrDefaultAsync(p => p.Id == result.Project.Id);
        Assert.NotNull(project);
        Assert.Equal(_testClient.Id, project.ClientId);
    }


    [Fact]
    public async Task CreateProjectAsync_InvalidSkill_ReturnsError()
    {
        // Arrange
        var createDto = new CreateProjectDto
        {
            Title = "Test Project",
            Description = "Test description",
            CreditBudget = 500,
            Deliverables = new List<CreateProjectDeliverableDto>
            {
                new CreateProjectDeliverableDto { Description = "Test deliverable" }
            },
            RequiredSkills = new List<CreateProjectSkillDto>
            {
                new CreateProjectSkillDto { SkillId = Guid.NewGuid(), ProficiencyRequired = 3 }
            }
        };

        // Act
        var result = await _service.CreateProjectAsync(createDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("skills are invalid", result.Message);
    }

    #endregion

    #region UpdateProjectAsync Tests

    [Fact]
    public async Task UpdateProjectAsync_ValidData_UpdatesProject()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Original Title",
            Description = "Original Description",
            CreditBudget = 300,
            Status = ProjectStatus.Draft,
            ModerationStatus = ModerationStatus.Pending
        };
        Context.Projects.Add(project);
        Context.SaveChanges();

        var updateDto = new UpdateProjectDto
        {
            Title = "Updated Title",
            Description = "Updated Description",
            CreditBudget = 600
        };

        // Act
        var result = await _service.UpdateProjectAsync(project.Id, updateDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal("Updated Title", result.Project.Title);
        Assert.Equal("Updated Description", result.Project.Description);
        Assert.Equal(600, result.Project.CreditBudget);
    }

    [Fact]
    public async Task UpdateProjectAsync_UnauthorizedUser_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Test Project",
            Description = "Test Description",
            CreditBudget = 300,
            Status = ProjectStatus.Draft
        };
        Context.Projects.Add(project);
        Context.SaveChanges();

        var otherUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "other@example.com",
            UserName = "other@example.com",
            Status = UserStatus.Active
        };
        Context.Users.Add(otherUser);
        Context.SaveChanges();

        var updateDto = new UpdateProjectDto { Title = "Hacked Title" };

        // Act
        var result = await _service.UpdateProjectAsync(project.Id, updateDto, otherUser.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("permission", result.Message);
    }

    [Fact]
    public async Task UpdateProjectAsync_PublishedProject_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Published Project",
            Description = "Published Description",
            CreditBudget = 300,
            Status = ProjectStatus.Published,
            ModerationStatus = ModerationStatus.Approved
        };
        Context.Projects.Add(project);
        Context.SaveChanges();

        var updateDto = new UpdateProjectDto { Title = "Updated Title" };

        // Act
        var result = await _service.UpdateProjectAsync(project.Id, updateDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not editable", result.Message);
    }

    #endregion

    #region SaveProjectDraftAsync Tests

    [Fact]
    public async Task SaveProjectDraftAsync_ValidData_CreatesDraft()
    {
        // Arrange
        var draftDto = new SaveDraftProjectDto
        {
            Title = "Draft Project",
            Description = "This is a draft",
            CreditBudget = 200
        };

        // Act
        var result = await _service.SaveProjectDraftAsync(draftDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal("Draft Project", result.Project.Title);
        Assert.Equal("Draft", result.Project.Status);

        var project = await Context.Projects.FindAsync(result.Project.Id);
        Assert.NotNull(project);
        Assert.Equal(ProjectStatus.Draft, project.Status);
    }

    [Fact]
    public async Task SaveProjectDraftAsync_MinimalData_CreatesValidDraft()
    {
        // Arrange
        var draftDto = new SaveDraftProjectDto();

        // Act
        var result = await _service.SaveProjectDraftAsync(draftDto, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Project);
        Assert.Equal("Untitled Project", result.Project.Title);
        Assert.Equal(100, result.Project.CreditBudget); // Default minimum
    }

    #endregion

    #region PublishProjectAsync Tests

    [Fact]
    public async Task PublishProjectAsync_ValidProject_PublishesProject()
    {
        // Arrange
        var project = CreateValidCompleteProject();

        // Act
        var result = await _service.PublishProjectAsync(project.Id, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("successfully", result.Message);

        var updatedProject = await Context.Projects.FindAsync(project.Id);
        Assert.Equal(ProjectStatus.Published, updatedProject!.Status);
        Assert.Equal(ModerationStatus.Pending, updatedProject.ModerationStatus);
    }

    [Fact]
    public async Task PublishProjectAsync_IncompleteProject_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = "Incomplete Project",
            Description = "",
            CreditBudget = 300,
            Status = ProjectStatus.Draft
        };
        Context.Projects.Add(project);
        Context.SaveChanges();

        // Act
        var result = await _service.PublishProjectAsync(project.Id, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("cannot be published", result.Message);
        Assert.Contains("Description is required", result.Message);
    }

    #endregion

    #region GetProjectByIdAsync Tests

    [Fact]
    public async Task GetProjectByIdAsync_PublicProject_ReturnsProject()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        project.Status = ProjectStatus.Published;
        project.ModerationStatus = ModerationStatus.Approved;
        Context.SaveChanges();

        // Act
        var result = await _service.GetProjectByIdAsync(project.Id, requestingUserId: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Title, result.Title);
        Assert.Equal(_testClient.Id, result.ClientId);
    }

    [Fact]
    public async Task GetProjectByIdAsync_DraftProject_ReturnsNullForPublicAccess()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        project.Status = ProjectStatus.Draft;
        Context.SaveChanges();

        // Act
        var result = await _service.GetProjectByIdAsync(project.Id, requestingUserId: null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetProjectByIdAsync_DraftProject_ReturnsProjectForPrivateAccess()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        project.Status = ProjectStatus.Draft;
        Context.SaveChanges();

        // Act
        var result = await _service.GetProjectByIdAsync(project.Id, requestingUserId: _testClient.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(project.Title, result.Title);
    }

    #endregion

    #region SearchProjectsAsync Tests

    [Fact]
    public async Task SearchProjectsAsync_WithQuery_ReturnsMatchingProjects()
    {
        // Arrange
        var project1 = CreateValidCompleteProject("React Development Project");
        project1.Status = ProjectStatus.Published;
        project1.ModerationStatus = ModerationStatus.Approved;

        var project2 = CreateValidCompleteProject("C# Backend System");
        project2.Status = ProjectStatus.Published;
        project2.ModerationStatus = ModerationStatus.Approved;

        Context.SaveChanges();

        var searchDto = new ProjectSearchDto
        {
            Query = "React",
            PublishedOnly = true
        };

        // Act
        var results = await _service.SearchProjectsAsync(searchDto);

        // Assert
        Assert.Single(results);
        Assert.Contains("React", results[0].Title);
    }

    [Fact]
    public async Task SearchProjectsAsync_WithSkillFilter_ReturnsMatchingProjects()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        project.Status = ProjectStatus.Published;
        project.ModerationStatus = ModerationStatus.Approved;

        var projectSkill = new ProjectSkill
        {
            ProjectId = project.Id,
            SkillId = _testSkill2.Id,
            ProficiencyRequired = SkillProficiency.Advanced,
            Weight = 5
        };
        Context.ProjectSkills.Add(projectSkill);
        Context.SaveChanges();

        var searchDto = new ProjectSearchDto
        {
            SkillIds = new List<Guid> { _testSkill2.Id },
            PublishedOnly = true
        };

        // Act
        var results = await _service.SearchProjectsAsync(searchDto);

        // Assert
        Assert.Single(results);
        Assert.Equal(project.Title, results[0].Title);
    }

    #endregion

    #region ValidateProjectRulesAsync Tests

    [Fact]
    public async Task ValidateProjectRulesAsync_ValidProject_ReturnsSuccess()
    {
        // Arrange
        var project = new Project
        {
            Title = "Valid Project",
            Description = "This is a valid project with proper description",
            CreditBudget = 500,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.ValidateProjectRulesAsync(project);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Validation passed", result.Message);
    }

    [Fact]
    public async Task ValidateProjectRulesAsync_InvalidBudget_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Title = "Invalid Budget Project",
            Description = "Valid description",
            CreditBudget = 25, // Below minimum
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30)
        };

        // Act
        var result = await _service.ValidateProjectRulesAsync(project);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Credit budget must be between 50 and 50,000", result.Message);
    }

    [Fact]
    public async Task ValidateProjectRulesAsync_InvalidTimeline_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Title = "Invalid Timeline Project",
            Description = "Valid description",
            CreditBudget = 500,
            StartDate = DateTime.UtcNow.AddDays(10),
            EndDate = DateTime.UtcNow.AddDays(5) // End before start
        };

        // Act
        var result = await _service.ValidateProjectRulesAsync(project);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("End date must be after start date", result.Message);
    }

    [Fact]
    public async Task ValidateProjectRulesAsync_PastEndDate_ReturnsError()
    {
        // Arrange
        var project = new Project
        {
            Title = "Past End Date Project",
            Description = "Valid description",
            CreditBudget = 500,
            StartDate = DateTime.UtcNow.AddDays(-10),
            EndDate = DateTime.UtcNow.AddDays(-5) // In the past
        };

        // Act
        var result = await _service.ValidateProjectRulesAsync(project);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("End date must be in the future", result.Message);
    }

    #endregion

    #region CanUserModifyProjectAsync Tests

    [Fact]
    public async Task CanUserModifyProjectAsync_ProjectOwner_ReturnsTrue()
    {
        // Arrange
        var project = CreateValidCompleteProject();

        // Act
        var result = await _service.CanUserModifyProjectAsync(project.Id, _testClient.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserModifyProjectAsync_NotOwner_ReturnsFalse()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        var otherUserId = Guid.NewGuid();

        // Act
        var result = await _service.CanUserModifyProjectAsync(project.Id, otherUserId);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region DeleteProjectAsync Tests

    [Fact]
    public async Task DeleteProjectAsync_ValidProject_MarksAsCancelled()
    {
        // Arrange
        var project = CreateValidCompleteProject();

        // Act
        var result = await _service.DeleteProjectAsync(project.Id, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.True(result.Success);

        var deletedProject = await Context.Projects.FindAsync(project.Id);
        Assert.Equal(ProjectStatus.Cancelled, deletedProject!.Status);
    }

    [Fact]
    public async Task DeleteProjectAsync_InProgressProject_ReturnsError()
    {
        // Arrange
        var project = CreateValidCompleteProject();
        project.Status = ProjectStatus.InProgress;
        Context.SaveChanges();

        // Act
        var result = await _service.DeleteProjectAsync(project.Id, _testClient.Id, "127.0.0.1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Cannot delete a project that is in progress", result.Message);
    }

    #endregion

    #region Helper Methods

    private Project CreateValidCompleteProject(string? title = null)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ClientId = _testClient.Id,
            Title = title ?? "Complete Test Project",
            Description = "This is a complete project with all required fields",
            CreditBudget = 500,
            StartDate = DateTime.UtcNow.AddDays(1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = ProjectStatus.Draft,
            ModerationStatus = ModerationStatus.Approved // Pre-approved for testing
        };

        Context.Projects.Add(project);

        // Add deliverable
        var deliverable = new ProjectDeliverable
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Description = "Complete the project deliverable",
            OrderIndex = 1,
            IsRequired = true
        };
        Context.ProjectDeliverables.Add(deliverable);

        // Add skill requirement
        var projectSkill = new ProjectSkill
        {
            ProjectId = project.Id,
            SkillId = _testSkill1.Id,
            ProficiencyRequired = SkillProficiency.Intermediate,
            Weight = 3
        };
        Context.ProjectSkills.Add(projectSkill);

        Context.SaveChanges();
        return project;
    }

    #endregion
}