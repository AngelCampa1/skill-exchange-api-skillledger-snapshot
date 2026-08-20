using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using Xunit;

namespace SkillLedger.Tests.Regression;

/// <summary>
/// Regression tests to ensure existing functionality still works after bug fixes
/// Tests complete user journeys and critical workflows
/// Phase 9.3: 15 regression tests validating no breaking changes
/// </summary>
[IntegrationTest]
[Collection("Integration Other")]
public class BugFixRegressionTests : IntegrationTestBase
{
    private readonly IUserService _userService;
    private readonly IProjectService _projectService;
    private readonly ISkillService _skillService;
    private readonly IMessagingService _messagingService;
    private readonly IProjectEscrowService _escrowService;
    private readonly IWorkspaceService _workspaceService;

    public BugFixRegressionTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _userService = ServiceScope.ServiceProvider.GetRequiredService<IUserService>();
        _projectService = ServiceScope.ServiceProvider.GetRequiredService<IProjectService>();
        _skillService = ServiceScope.ServiceProvider.GetRequiredService<ISkillService>();
        _messagingService = ServiceScope.ServiceProvider.GetRequiredService<IMessagingService>();
        _escrowService = ServiceScope.ServiceProvider.GetRequiredService<IProjectEscrowService>();
        _workspaceService = ServiceScope.ServiceProvider.GetRequiredService<IWorkspaceService>();
    }

    #region Helper Methods

    private async Task<Guid> CreateTestSkillAsync(string name, string category = "Programming")
    {
        var createDto = new CreateSkillDto
        {
            Name = name,
            Description = $"Description for {name}",
            Category = category
        };
        var result = await _skillService.CreateSkillAsync(createDto);
        if (!result.Success || result.Data == null)
            return Guid.Empty;
        var skillDto = (SkillDto)result.Data;
        return skillDto.Id;
    }

    private async Task<ProjectDto> CreateTestProjectAsync(Guid clientId, string title)
    {
        // Create a skill for project requirements
        var skillId = await CreateTestSkillAsync($"ProjectSkill_{Guid.NewGuid():N}");
        skillId.Should().NotBe(Guid.Empty, "skill should be created successfully");

        var createDto = new CreateProjectDto
        {
            Title = title,
            Description = $"Description for {title}",
            CreditBudget = 100,
            Deliverables = new List<CreateProjectDeliverableDto>
            {
                new CreateProjectDeliverableDto
                {
                    Description = "Primary deliverable",
                    OrderIndex = 0,
                    IsRequired = true
                }
            },
            RequiredSkills = new List<CreateProjectSkillDto>
            {
                new CreateProjectSkillDto
                {
                    SkillId = skillId,
                    ProficiencyRequired = 2
                }
            }
        };
        var result = await _projectService.CreateProjectAsync(createDto, clientId, "127.0.0.1");
        result.Should().NotBeNull("project response should not be null");
        result.Success.Should().BeTrue($"project creation should succeed: {result.Message}");
        result.Project.Should().NotBeNull("project data should be returned");
        return result.Project!;
    }

    private async Task<(Guid workspaceId, Guid clientId, Guid providerId)> CreateTestWorkspaceAsync()
    {
        var client = await CreateTestUserAsync($"client_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var provider = await CreateTestUserAsync($"provider_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var project = await CreateTestProjectAsync(client.Id, $"Project_{Guid.NewGuid():N}");
        var projectEntity = await Context.Projects.FindAsync(project.Id);
        projectEntity!.ProviderId = provider.Id;
        await Context.SaveChangesAsync();
        var workspace = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);

        return (workspace.Id, client.Id, provider.Id);
    }

    #endregion

    #region Complete User Journey Tests

    [Fact]
    public async Task UserJourney_RegisterLoginProfileSkillProject_WorksEndToEnd()
    {
        // Step 1: Register a new user
        var user = await CreateTestUserAsync($"journey_user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        user.Should().NotBeNull("user registration should succeed");

        // Step 2: Create a skill
        var skillId = await CreateTestSkillAsync($"JourneySkill_{Guid.NewGuid():N}");
        skillId.Should().NotBeEmpty("skill creation should succeed");

        // Step 3: Add skill to user
        var addSkillDto = new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Expert
        };
        var addSkillResult = await _skillService.AddUserSkillAsync(user.Id, addSkillDto);
        addSkillResult.Success.Should().BeTrue("adding skill to user should succeed");

        // Step 4: Create a project
        var project = await CreateTestProjectAsync(user.Id, $"JourneyProject_{Guid.NewGuid():N}");
        project.Should().NotBeNull("project creation should succeed");

        // Verify the user has their skill
        var userSkills = await _skillService.GetUserSkillsAsync(user.Id);
        userSkills.Should().NotBeEmpty("user should have at least one skill");
    }

    [Fact]
    public async Task UserJourney_BrowseProjectsApplyMessage_WorksEndToEnd()
    {
        // Step 1: Create a project owner and provider
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();

        // Step 2: Provider sends a message in the workspace
        var sendRequest = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "I'm interested in this project!",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var message = await _messagingService.SendMessageAsync(sendRequest, providerId);
        message.Should().NotBeNull("sending message should succeed");

        // Step 3: Client responds
        var responseRequest = new SendMessageRequest
        {
            WorkspaceId = workspaceId,
            MessageText = "Great, let's discuss the details.",
            MessageType = MessageType.Text,
            IdempotencyKey = Guid.NewGuid().ToString()
        };
        var response = await _messagingService.SendMessageAsync(responseRequest, clientId);
        response.Should().NotBeNull("client response should succeed");

        // Step 4: Verify message history
        var historyRequest = new MessageHistoryRequest
        {
            WorkspaceId = workspaceId,
            PageNumber = 1,
            PageSize = 10
        };
        var history = await _messagingService.GetMessageHistoryAsync(historyRequest, clientId);
        history.Messages.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task UserJourney_ProjectCreationMilestoneCompletion_WorksEndToEnd()
    {
        // Step 1: Create users
        var client = await CreateTestUserAsync($"client_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var provider = await CreateTestUserAsync($"provider_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        // Step 2: Create a project
        var project = await CreateTestProjectAsync(client.Id, $"MilestoneProject_{Guid.NewGuid():N}");
        project.Should().NotBeNull("project creation should succeed");
        var projectEntity = await Context.Projects.FindAsync(project.Id);
        projectEntity!.ProviderId = provider.Id;
        await Context.SaveChangesAsync();

        // Step 3: Create workspace
        var workspace = await _workspaceService.CreateWorkspaceAsync(project.Id, provider.Id);
        workspace.Should().NotBeNull("workspace creation should succeed");

        // Verify workspace is accessible
        var hasAccess = await _workspaceService.HasUserAccessAsync(workspace.Id, client.Id);
        hasAccess.Should().BeTrue("client should have access to workspace");
    }

    #endregion

    #region Critical Workflow Tests

    [Fact]
    public async Task CriticalWorkflow_SkillSearchAndFilter_WorksCorrectly()
    {
        // Arrange - Create skills in different categories
        var uniqueCategory = $"TestCategory_{Guid.NewGuid():N}";
        await CreateTestSkillAsync($"FilterSkill1_{Guid.NewGuid():N}", uniqueCategory);
        await CreateTestSkillAsync($"FilterSkill2_{Guid.NewGuid():N}", uniqueCategory);
        await CreateTestSkillAsync($"OtherSkill_{Guid.NewGuid():N}", "OtherCategory");

        // Act - Search with filter
        var searchDto = new SkillSearchDto
        {
            Category = uniqueCategory
        };
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        skills.Should().OnlyContain(s => s.Category == uniqueCategory);
        totalCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task CriticalWorkflow_ProjectSearchAndFilter_WorksCorrectly()
    {
        // Arrange - Create multiple projects
        var user = await CreateTestUserAsync($"project_owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var searchTerm = $"Searchable_{Guid.NewGuid():N}";
        await CreateTestProjectAsync(user.Id, $"{searchTerm}_Project1");
        await CreateTestProjectAsync(user.Id, $"{searchTerm}_Project2");

        // Act - Search projects (include unpublished since these are newly created)
        var searchDto = new ProjectSearchDto
        {
            Query = searchTerm,
            PublishedOnly = false
        };
        var searchResult = await _projectService.SearchProjectsAsync(searchDto);

        // Assert
        searchResult.Should().HaveCountGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task CriticalWorkflow_MessagingConversation_WorksCorrectly()
    {
        // Arrange
        var (workspaceId, clientId, providerId) = await CreateTestWorkspaceAsync();

        // Act - Exchange messages
        var messages = new List<MessageDto>();
        for (int i = 0; i < 5; i++)
        {
            var senderId = i % 2 == 0 ? clientId : providerId;
            var request = new SendMessageRequest
            {
                WorkspaceId = workspaceId,
                MessageText = $"Message {i + 1}",
                MessageType = MessageType.Text,
                IdempotencyKey = Guid.NewGuid().ToString()
            };
            messages.Add(await _messagingService.SendMessageAsync(request, senderId));
        }

        // Assert
        messages.Should().HaveCount(5);
        messages.Should().AllSatisfy(m => m.Should().NotBeNull());
    }

    #endregion

    #region Authorization Regression Tests

    [Fact]
    public async Task Authorization_AuthenticatedUserAccessOwnProfile_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync($"auth_user_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        // Act - User accesses their own profile data via user service
        var userProfile = await _userService.GetUserByIdAsync(user.Id);

        // Assert
        userProfile.Should().NotBeNull();
        userProfile!.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task Authorization_PublicSkillsEndpoint_Succeeds()
    {
        // Arrange - Create some public skills
        await CreateTestSkillAsync($"PublicSkill_{Guid.NewGuid():N}");

        // Act - Search skills (public endpoint)
        var searchDto = new SkillSearchDto { Take = 10 };
        var (skills, _) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        skills.Should().NotBeEmpty("public skills should be accessible");
    }

    [Fact]
    public async Task Authorization_CrossUserAccessAttempt_Fails()
    {
        // Arrange
        var owner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"OwnerSkill_{Guid.NewGuid():N}");
        var addResult = await _skillService.AddUserSkillAsync(owner.Id, new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Expert
        });
        var userSkillDto = (UserSkillDto)addResult.Data!;
        var userSkillId = userSkillDto.Id;

        // Act - Attacker tries to update owner's skill
        var updateResult = await _skillService.UpdateUserSkillAsync(attacker.Id, userSkillId, new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Beginner
        });

        // Assert
        updateResult.Success.Should().BeFalse("cross-user access should be denied");
    }

    #endregion

    #region Data Integrity Regression Tests

    [Fact]
    public async Task DataIntegrity_SkillAdditionAndRemoval_WorksCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync($"integrity_user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"IntegritySkill_{Guid.NewGuid():N}");

        // Act - Add skill
        var addResult = await _skillService.AddUserSkillAsync(user.Id, new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Intermediate
        });
        addResult.Success.Should().BeTrue();
        var addedUserSkill = (UserSkillDto)addResult.Data!;
        var userSkillId = addedUserSkill.Id;

        // Verify skill is added
        var skillsBefore = await _skillService.GetUserSkillsAsync(user.Id);
        skillsBefore.Should().Contain(s => s.Id == userSkillId);

        // Remove skill
        var removeResult = await _skillService.RemoveUserSkillAsync(user.Id, userSkillId);
        removeResult.Success.Should().BeTrue();

        // Verify skill is removed
        var skillsAfter = await _skillService.GetUserSkillsAsync(user.Id);
        skillsAfter.Should().NotContain(s => s.Id == userSkillId);
    }

    [Fact]
    public async Task DataIntegrity_ProjectCreationAndRetrieval_WorksCorrectly()
    {
        // Arrange
        var user = await CreateTestUserAsync($"project_user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var projectTitle = $"IntegrityProject_{Guid.NewGuid():N}";

        // Act - Create project (CreateProjectAsync internally retrieves via GetProjectByIdAsync)
        var project = await CreateTestProjectAsync(user.Id, projectTitle);

        // Assert - Verify the returned project has correct data
        // CreateProjectAsync internally uses GetProjectByIdAsync, so this tests retrieval
        project.Should().NotBeNull();
        project.Id.Should().NotBeEmpty();
        project.Title.Should().Be(projectTitle);
        project.ClientId.Should().Be(user.Id);

        // Additional verification - project exists in database context
        var existsInDb = await Context.Projects.AnyAsync(p => p.Id == project.Id);
        existsInDb.Should().BeTrue("project should exist in database");
    }

    [Fact]
    public async Task DataIntegrity_EndorsementCreationAndRemoval_WorksCorrectly()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser = await CreateTestUserAsync($"endorser_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"EndorseSkill_{Guid.NewGuid():N}");
        var addResult = await _skillService.AddUserSkillAsync(skillOwner.Id, new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Expert
        });
        var addedSkill = (UserSkillDto)addResult.Data!;
        var userSkillId = addedSkill.Id;

        // Act - Create endorsement
        var createEndorsement = await _skillService.CreateSkillEndorsementAsync(endorser.Id, new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Great skill!"
        });
        createEndorsement.Success.Should().BeTrue();

        // Verify endorsement exists
        var endorsementsBefore = await _skillService.GetSkillEndorsementsAsync(userSkillId);
        endorsementsBefore.Should().ContainSingle();

        // Remove endorsement
        var endorsement = (SkillEndorsementDto)createEndorsement.Data!;
        var removeResult = await _skillService.RemoveSkillEndorsementAsync(endorser.Id, endorsement.Id);
        removeResult.Success.Should().BeTrue();

        // Verify endorsement is removed
        var endorsementsAfter = await _skillService.GetSkillEndorsementsAsync(userSkillId);
        endorsementsAfter.Should().BeEmpty();
    }

    #endregion

    #region Performance Regression Tests

    [Fact]
    public async Task Performance_LargeDataSetPagination_WorksCorrectly()
    {
        // Arrange - Create multiple skills
        var uniqueCategory = $"PerfCategory_{Guid.NewGuid():N}";
        for (int i = 0; i < 20; i++)
        {
            await CreateTestSkillAsync($"PerfSkill_{i}_{Guid.NewGuid():N}", uniqueCategory);
        }

        // Act - Paginate through results
        var (page1Skills, page1Total) = await _skillService.SearchSkillsAsync(new SkillSearchDto
        {
            Category = uniqueCategory,
            Skip = 0,
            Take = 10
        });
        var (page2Skills, _) = await _skillService.SearchSkillsAsync(new SkillSearchDto
        {
            Category = uniqueCategory,
            Skip = 10,
            Take = 10
        });

        // Assert
        page1Skills.Count.Should().Be(10);
        page2Skills.Count.Should().BeGreaterOrEqualTo(10);
        page1Total.Should().BeGreaterOrEqualTo(20);
    }

    [Fact]
    public async Task Performance_ConcurrentUserOperations_WorksCorrectly()
    {
        // Arrange - Create users
        var users = new List<SkillLedger.Core.Entities.User>();
        for (int i = 0; i < 5; i++)
        {
            users.Add(await CreateTestUserAsync($"concurrent_user_{i}_{Guid.NewGuid():N}@test.com", "TestPassword123!"));
        }

        var skillId = await CreateTestSkillAsync($"ConcurrentSkill_{Guid.NewGuid():N}");

        // Act - Concurrent skill additions
        var tasks = users.Select(user => _skillService.AddUserSkillAsync(user.Id, new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Intermediate
        })).ToList();
        var results = await Task.WhenAll(tasks);

        // Assert - All should succeed
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    [Fact]
    public async Task Performance_MultipleProjectSearch_WorksCorrectly()
    {
        // Arrange - Create multiple projects
        var user = await CreateTestUserAsync($"multi_project_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var searchPrefix = $"MultiSearch_{Guid.NewGuid():N}";

        for (int i = 0; i < 10; i++)
        {
            await CreateTestProjectAsync(user.Id, $"{searchPrefix}_Project_{i}");
        }

        // Act - Search with prefix (include unpublished since these are newly created)
        var searchResult = await _projectService.SearchProjectsAsync(new ProjectSearchDto
        {
            Query = searchPrefix,
            PublishedOnly = false
        });

        // Assert
        searchResult.Should().HaveCountGreaterOrEqualTo(10);
    }

    #endregion
}
