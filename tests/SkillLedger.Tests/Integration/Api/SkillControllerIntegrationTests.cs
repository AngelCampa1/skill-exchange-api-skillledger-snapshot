using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for SkillController API endpoints
/// Tests CRITICAL authorization issues and Phase 3 & 4 bug fixes
/// Validates BUG-SKILL-001 through BUG-SKILL-029 fixes
/// </summary>
[IntegrationTest]
[Collection("Integration Api 2")]
public class SkillControllerIntegrationTests : IntegrationTestBase
{
    private readonly ISkillService _skillService;

    public SkillControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _skillService = ServiceScope.ServiceProvider.GetRequiredService<ISkillService>();
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
        result.Success.Should().BeTrue($"creating skill {name} should succeed");
        var skillDto = (SkillDto)result.Data!;
        return skillDto.Id;
    }

    private async Task<Guid> AddSkillToUserAsync(Guid userId, Guid skillId, SkillProficiency proficiency = SkillProficiency.Intermediate)
    {
        var addDto = new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = proficiency
        };
        var result = await _skillService.AddUserSkillAsync(userId, addDto);
        result.Success.Should().BeTrue("adding skill to user should succeed");
        var userSkillDto = (UserSkillDto)result.Data!;
        return userSkillDto.Id;
    }

    #endregion

    #region CRITICAL - Authorization Tests (6 endpoints from audit)

    [Fact]
    public async Task AddEndorsement_AuthenticatedUser_Succeeds()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser = await CreateTestUserAsync($"endorser_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        // Act
        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Great skill!"
        };
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, endorsementDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveEndorsement_AuthenticatedEndorser_Succeeds()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser = await CreateTestUserAsync($"endorser_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        // Create endorsement first
        var createEndorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Endorsement to remove"
        };
        var createResult = await _skillService.CreateSkillEndorsementAsync(endorser.Id, createEndorsementDto);
        createResult.Success.Should().BeTrue();

        // Act
        var createdEndorsementDto = (SkillEndorsementDto)createResult.Data!;
        var removeResult = await _skillService.RemoveSkillEndorsementAsync(endorser.Id, createdEndorsementDto.Id);

        // Assert
        removeResult.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSkill_ByOwner_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(user.Id, skillId, SkillProficiency.Beginner);

        // Act
        var updateDto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert,
            IsVisible = true
        };
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkillId, updateDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSkillVisibility_ByOwner_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(user.Id, skillId);

        // Act - Set visibility to false
        var updateDto = new UpdateUserSkillDto
        {
            IsVisible = false
        };
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkillId, updateDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSkill_ByNonOwner_Fails()
    {
        // Arrange
        var owner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(owner.Id, skillId);

        // Act - Attacker tries to update owner's skill
        var updateDto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert
        };
        var result = await _skillService.UpdateUserSkillAsync(attacker.Id, userSkillId, updateDto);

        // Assert
        result.Success.Should().BeFalse("non-owner should not be able to update another user's skill");
    }

    [Fact]
    [SecurityTest]
    public async Task CompatibilityAddUserSkill_CrossUser_ReturnsForbidden()
    {
        var owner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"CompatAdd_{Guid.NewGuid():N}");
        AuthenticateAs(attacker);

        var content = JsonContent.Create(new AddUserSkillDto
        {
            SkillId = skillId,
            Proficiency = SkillProficiency.Expert
        });
        await AddCsrfTokenToRequest(content);

        var response = await Client.PostAsync($"/api/skills/users/{owner.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task CompatibilityUpdateUserSkill_CrossUser_ReturnsForbidden()
    {
        var owner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"CompatUpdate_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(owner.Id, skillId);
        AuthenticateAs(attacker);

        var content = JsonContent.Create(new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert
        });
        await AddCsrfTokenToRequest(content);

        var response = await Client.PutAsync($"/api/skills/users/{owner.Id}/{userSkillId}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task CompatibilityRemoveUserSkill_CrossUser_ReturnsForbidden()
    {
        var owner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"CompatRemove_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(owner.Id, skillId);
        AuthenticateAs(attacker);

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/skills/users/{owner.Id}/{userSkillId}");
        request.Headers.Add("X-CSRF-TOKEN", await GetCsrfTokenAsync());

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    [SecurityTest]
    public async Task CompatibilityCreateEndorsement_SpoofedEndorser_ReturnsForbidden()
    {
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var spoofedEndorser = await CreateTestUserAsync($"spoofed_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var attacker = await CreateTestUserAsync($"attacker_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"CompatEndorse_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);
        AuthenticateAs(attacker);

        var content = JsonContent.Create(new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Spoofed endorsement attempt"
        });
        await AddCsrfTokenToRequest(content);

        var response = await Client.PostAsync($"/api/skills/endorsements/{spoofedEndorser.Id}", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveUserSkill_ByOwner_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(user.Id, skillId);

        // Act
        var result = await _skillService.RemoveUserSkillAsync(user.Id, userSkillId);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Self-Endorsement Prevention

    [Fact]
    public async Task AddEndorsement_SelfEndorse_Fails()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"TestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(user.Id, skillId);

        // Act - User tries to endorse their own skill
        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Self-endorsement attempt"
        };
        var result = await _skillService.CreateSkillEndorsementAsync(user.Id, endorsementDto);

        // Assert
        result.Success.Should().BeFalse("users should not be able to endorse their own skills");
    }

    #endregion

    #region CSRF Protection Tests

    [Fact]
    public async Task AddEndorsement_ValidRequest_Succeeds()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser = await CreateTestUserAsync($"endorser_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"CSRFTestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        // Act
        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkillId,
            Comment = "Valid endorsement"
        };
        var result = await _skillService.CreateSkillEndorsementAsync(endorser.Id, endorsementDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateUserSkill_ValidRequest_Succeeds()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId = await CreateTestSkillAsync($"CSRFTestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(user.Id, skillId);

        // Act
        var updateDto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert
        };
        var result = await _skillService.UpdateUserSkillAsync(user.Id, userSkillId, updateDto);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveEndorsement_ValidRequest_Succeeds()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser = await CreateTestUserAsync($"endorser_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"CSRFTestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        var createEndorsementDto = new CreateSkillEndorsementDto { UserSkillId = userSkillId };
        var createResult = await _skillService.CreateSkillEndorsementAsync(endorser.Id, createEndorsementDto);

        // Act
        var createdEndorsement = (SkillEndorsementDto)createResult.Data!;
        var result = await _skillService.RemoveSkillEndorsementAsync(endorser.Id, createdEndorsement.Id);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task AddEndorsement_MultipleEndorsements_AllSucceed()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorsers = new List<SkillLedger.Core.Entities.User>();
        for (int i = 0; i < 5; i++)
        {
            endorsers.Add(await CreateTestUserAsync($"endorser{i}_{Guid.NewGuid():N}@test.com", "TestPassword123!"));
        }

        var skillId = await CreateTestSkillAsync($"RateLimitTestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        // Act - Multiple endorsers endorse the same skill
        var results = new List<ServiceResponseDto>();
        foreach (var endorser in endorsers)
        {
            var endorsementDto = new CreateSkillEndorsementDto
            {
                UserSkillId = userSkillId,
                Comment = $"Endorsement from {endorser.Email}"
            };
            results.Add(await _skillService.CreateSkillEndorsementAsync(endorser.Id, endorsementDto));
        }

        // Assert - All should succeed
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
    }

    #endregion

    #region GET Endpoint Tests

    [Fact]
    public async Task SearchSkills_ReturnsAllSkills()
    {
        // Arrange
        var skillName1 = $"SearchSkill1_{Guid.NewGuid():N}";
        var skillName2 = $"SearchSkill2_{Guid.NewGuid():N}";
        await CreateTestSkillAsync(skillName1);
        await CreateTestSkillAsync(skillName2);

        // Act
        var searchDto = new SkillSearchDto { Take = 100 };
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        totalCount.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetSkillById_ValidId_ReturnsSkill()
    {
        // Arrange
        var skillName = $"GetByIdSkill_{Guid.NewGuid():N}";
        var skillId = await CreateTestSkillAsync(skillName);

        // Act
        var skill = await _skillService.GetSkillByIdAsync(skillId);

        // Assert
        skill.Should().NotBeNull();
        skill!.Name.Should().Be(skillName);
    }

    [Fact]
    public async Task GetSkillById_InvalidId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var skill = await _skillService.GetSkillByIdAsync(nonExistentId);

        // Assert
        skill.Should().BeNull();
    }

    [Fact]
    public async Task GetUserSkills_ReturnsUserSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skillId1 = await CreateTestSkillAsync($"UserSkill1_{Guid.NewGuid():N}");
        var skillId2 = await CreateTestSkillAsync($"UserSkill2_{Guid.NewGuid():N}");
        await AddSkillToUserAsync(user.Id, skillId1);
        await AddSkillToUserAsync(user.Id, skillId2);

        // Act
        var userSkills = await _skillService.GetUserSkillsAsync(user.Id);

        // Assert
        userSkills.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSkillEndorsements_ReturnsEndorsements()
    {
        // Arrange
        var skillOwner = await CreateTestUserAsync($"owner_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser1 = await CreateTestUserAsync($"endorser1_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var endorser2 = await CreateTestUserAsync($"endorser2_{Guid.NewGuid():N}@test.com", "TestPassword123!");

        var skillId = await CreateTestSkillAsync($"EndorsementTestSkill_{Guid.NewGuid():N}");
        var userSkillId = await AddSkillToUserAsync(skillOwner.Id, skillId);

        await _skillService.CreateSkillEndorsementAsync(endorser1.Id, new CreateSkillEndorsementDto { UserSkillId = userSkillId });
        await _skillService.CreateSkillEndorsementAsync(endorser2.Id, new CreateSkillEndorsementDto { UserSkillId = userSkillId });

        // Act
        var endorsements = await _skillService.GetSkillEndorsementsAsync(userSkillId);

        // Assert
        endorsements.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSkillCategories_ReturnsCategories()
    {
        // Arrange - Create skills in different categories
        await CreateTestSkillAsync($"ProgrammingSkill_{Guid.NewGuid():N}", "Programming");
        await CreateTestSkillAsync($"DesignSkill_{Guid.NewGuid():N}", "Design");

        // Act
        var categories = await _skillService.GetSkillCategoriesAsync();

        // Assert
        categories.Should().NotBeEmpty();
    }

    #endregion

    #region Search and Filter Tests

    [Fact]
    public async Task SearchSkills_WithQuery_ReturnsMatchingSkills()
    {
        // Arrange
        var uniquePrefix = Guid.NewGuid().ToString("N")[..8];
        var skillName = $"SearchableSkill_{uniquePrefix}";
        await CreateTestSkillAsync(skillName);

        // Act
        var searchDto = new SkillSearchDto
        {
            Query = uniquePrefix
        };
        var (skills, _) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        skills.Should().ContainSingle(s => s.Name.Contains(uniquePrefix));
    }

    [Fact]
    public async Task SearchSkills_ByCategory_ReturnsFilteredSkills()
    {
        // Arrange
        var category = $"TestCategory_{Guid.NewGuid():N}";
        await CreateTestSkillAsync($"CategorySkill1_{Guid.NewGuid():N}", category);
        await CreateTestSkillAsync($"CategorySkill2_{Guid.NewGuid():N}", category);
        await CreateTestSkillAsync($"OtherSkill_{Guid.NewGuid():N}", "OtherCategory");

        // Act
        var searchDto = new SkillSearchDto
        {
            Category = category
        };
        var (skills, _) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        skills.Should().OnlyContain(s => s.Category == category);
    }

    [Fact]
    public async Task SearchSkills_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange - Create multiple skills
        for (int i = 0; i < 10; i++)
        {
            await CreateTestSkillAsync($"PaginationSkill_{i}_{Guid.NewGuid():N}");
        }

        // Act
        var searchDto = new SkillSearchDto
        {
            Skip = 0,
            Take = 5
        };
        var (skills, totalCount) = await _skillService.SearchSkillsAsync(searchDto);

        // Assert
        skills.Count.Should().BeLessOrEqualTo(5);
        totalCount.Should().BeGreaterOrEqualTo(10);
    }

    [Fact]
    public async Task GetUserSkills_VisibleOnly_ReturnsOnlyVisibleSkills()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var visibleSkillId = await CreateTestSkillAsync($"VisibleSkill_{Guid.NewGuid():N}");
        var hiddenSkillId = await CreateTestSkillAsync($"HiddenSkill_{Guid.NewGuid():N}");

        var visibleUserSkillId = await AddSkillToUserAsync(user.Id, visibleSkillId);
        var hiddenUserSkillId = await AddSkillToUserAsync(user.Id, hiddenSkillId);

        // Hide one skill
        await _skillService.UpdateUserSkillAsync(user.Id, hiddenUserSkillId, new UpdateUserSkillDto { IsVisible = false });

        // Act
        var visibleSkills = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: true);
        var allSkills = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: false);

        // Assert
        visibleSkills.Should().HaveCountLessThan(allSkills.Count);
    }

    [Fact]
    public async Task GetUserSkills_Owner_ReturnsAllSkillsIncludingHidden()
    {
        // Arrange
        var user = await CreateTestUserAsync($"user_{Guid.NewGuid():N}@test.com", "TestPassword123!");
        var skill1Id = await CreateTestSkillAsync($"Skill1_{Guid.NewGuid():N}");
        var skill2Id = await CreateTestSkillAsync($"Skill2_{Guid.NewGuid():N}");

        await AddSkillToUserAsync(user.Id, skill1Id);
        var hiddenUserSkillId = await AddSkillToUserAsync(user.Id, skill2Id);

        // Hide one skill
        await _skillService.UpdateUserSkillAsync(user.Id, hiddenUserSkillId, new UpdateUserSkillDto { IsVisible = false });

        // Act - Owner requests all skills (not visible only)
        var allSkills = await _skillService.GetUserSkillsAsync(user.Id, visibleOnly: false);

        // Assert
        allSkills.Should().HaveCount(2, "owner should see all their skills including hidden ones");
    }

    #endregion
}
