using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using SkillLedger.Api;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using static SkillLedger.Tests.Infrastructure.TestJsonOptions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration;

[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class SkillApiIntegrationTests : IntegrationTestBase
{
    private User _testUser = null!;

    public SkillApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    #region CSRF-Protected Request Helpers

    /// <summary>
    /// Sends a POST request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> PostWithCsrfAsync<T>(string url, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.PostAsync(url, content);
    }

    /// <summary>
    /// Sends a PUT request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> PutWithCsrfAsync<T>(string url, T payload)
    {
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var csrfToken = await GetCsrfTokenAsync();
        content.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.PutAsync(url, content);
    }

    /// <summary>
    /// Sends a DELETE request with CSRF token included
    /// </summary>
    private async Task<HttpResponseMessage> DeleteWithCsrfAsync(string url)
    {
        var csrfToken = await GetCsrfTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("X-CSRF-TOKEN", csrfToken);
        return await Client.SendAsync(request);
    }

    #endregion

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "testuser@example.com",
            PasswordHash = "hashedpassword",
            Status = UserStatus.Active
        };

        Context.Users.Add(_testUser);
        await Context.SaveChangesAsync();
    }

    [Fact]
    [FastTest]
    public async Task CreateSkill_WithValidData_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var createSkillDto = new CreateSkillDto
        {
            Name = "Integration Test Skill",
            Description = "A skill created during integration testing",
            Category = "Testing"
        };

        // Act
        var response = await PostWithCsrfAsync("/api/skill", createSkillDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        // Debug: Check if skill was created in database
        var skillsInDb = Context.Skills.Count();
        Console.WriteLine($"Skills in test database: {skillsInDb}");

        var location = response.Headers.Location?.ToString();
        Console.WriteLine($"Location header: {location}");
        Assert.NotNull(location);

        var getResponse = await Client.GetAsync(location);
        Console.WriteLine($"GET response status: {getResponse.StatusCode}");
        if (getResponse.StatusCode != HttpStatusCode.OK)
        {
            var errorContent = await getResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"GET response content: {errorContent}");
        }
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var skill = await getResponse.Content.ReadFromJsonAsync<SkillDto>(TestJsonOptions.Default);
        Assert.NotNull(skill);
        Assert.Equal("Integration Test Skill", skill.Name);
        Assert.Equal("Testing", skill.Category);
    }

    [Fact]
    [FastTest]
    public async Task GetSkills_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        AuthenticateAs(_testUser);
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "C#", Category = "Programming" });
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "Photoshop", Category = "Design" });
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "Java", Category = "Programming" });

        // Act
        var response = await Client.GetAsync("/api/skill?category=Programming&take=10");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SkillSearchResultDto>(content, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Skills, skill => Assert.Equal("Programming", skill.Category));
    }

    [Fact]
    [FastTest]
    public async Task AddUserSkill_WithValidData_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        Context.Skills.Add(skill);
        await Context.SaveChangesAsync();

        var addUserSkillDto = new AddUserSkillDto
        {
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Intermediate,
            YearsOfExperience = 3,
            Notes = "Added through integration test"
        };

        // Act
        var response = await PostWithCsrfAsync($"/api/skill/users/{_testUser.Id}", addUserSkillDto);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var userSkill = await response.Content.ReadFromJsonAsync<UserSkillDto>(TestJsonOptions.Default);
        Assert.NotNull(userSkill);
        Assert.Equal(_testUser.Id, userSkill.UserId);
        Assert.Equal(SkillProficiency.Intermediate, userSkill.Proficiency);
        Assert.Equal(3, userSkill.YearsOfExperience);
    }

    [Fact]
    [FastTest]
    public async Task UpdateUserSkill_WithValidData_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        var userSkill = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Beginner,
            YearsOfExperience = 1
        };

        Context.Skills.Add(skill);
        Context.UserSkills.Add(userSkill);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateUserSkillDto
        {
            Proficiency = SkillProficiency.Expert,
            YearsOfExperience = 10,
            Notes = "Updated through integration test"
        };

        // Act
        var response = await PutWithCsrfAsync($"/api/skill/users/{_testUser.Id}/{userSkill.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedSkill = await response.Content.ReadFromJsonAsync<UserSkillDto>(TestJsonOptions.Default);
        Assert.NotNull(updatedSkill);
        Assert.Equal(SkillProficiency.Expert, updatedSkill.Proficiency);
        Assert.Equal(10, updatedSkill.YearsOfExperience);
    }

    [Fact]
    [FastTest]
    public async Task CreateSkillEndorsement_WithValidData_ReturnsCreated()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skillOwner = new User { Email = "owner@example.com", PasswordHash = "hash" };
        var endorser = new User { Email = "endorser@example.com", PasswordHash = "hash" };
        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        Context.Users.AddRange(skillOwner, endorser);
        Context.Skills.Add(skill);
        await Context.SaveChangesAsync();

        var userSkill = new UserSkill
        {
            UserId = skillOwner.Id,
            SkillId = skill.Id,
            User = skillOwner,
            Skill = skill,
            Proficiency = SkillProficiency.Advanced
        };

        Context.UserSkills.Add(userSkill);
        await Context.SaveChangesAsync();

        var endorsementDto = new CreateSkillEndorsementDto
        {
            UserSkillId = userSkill.Id,
            ReviewText = "Great skills demonstrated!"
        };

        // Debug: Check what's in the database
        var userSkillInDb = Context.UserSkills.Count();
        var endorserInDb = Context.Users.Any(u => u.Id == endorser.Id);
        Console.WriteLine($"UserSkills in test database: {userSkillInDb}");
        Console.WriteLine($"Endorser exists: {endorserInDb}");
        Console.WriteLine($"UserSkill ID being used: {userSkill.Id}");

        // Act
        var response = await PostWithCsrfAsync($"/api/skill/endorsements/{endorser.Id}", endorsementDto);

        // Debug: Check response
        Console.WriteLine($"Response status: {response.StatusCode}");
        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Response content: {responseContent}");

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var endorsement = await response.Content.ReadFromJsonAsync<SkillEndorsementDto>(TestJsonOptions.Default);
        Assert.NotNull(endorsement);
        Assert.Equal(endorser.Id, endorsement.EndorsedByUser.Id);
        Assert.Equal("Great skills demonstrated!", endorsement.ReviewText);
    }

    [Fact]
    [FastTest]
    public async Task GetUserSkills_ReturnsUserSkillsList()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill1 = new Skill { Name = "C#", Category = "Programming" };
        var skill2 = new Skill { Name = "React", Category = "Programming" };

        Context.Skills.AddRange(skill1, skill2);
        await Context.SaveChangesAsync();

        var userSkill1 = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill1.Id,
            Skill = skill1,
            User = _testUser,
            Proficiency = SkillProficiency.Advanced,
            IsVisible = true
        };
        var userSkill2 = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill2.Id,
            Skill = skill2,
            User = _testUser,
            Proficiency = SkillProficiency.Intermediate,
            IsVisible = false
        };

        Context.UserSkills.AddRange(userSkill1, userSkill2);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/skill/users/{_testUser.Id}?visibleOnly=false");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userSkills = await response.Content.ReadFromJsonAsync<List<UserSkillDto>>(TestJsonOptions.Default);
        Assert.NotNull(userSkills);
        Assert.Equal(2, userSkills.Count);
    }

    [Fact]
    [FastTest]
    public async Task GetUserSkills_WithVisibleOnlyFilter_ReturnsOnlyVisibleSkills()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill1 = new Skill { Name = "C#", Category = "Programming" };
        var skill2 = new Skill { Name = "React", Category = "Programming" };
        var userSkill1 = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill1.Id,
            Proficiency = SkillProficiency.Advanced,
            IsVisible = true
        };
        var userSkill2 = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill2.Id,
            Proficiency = SkillProficiency.Intermediate,
            IsVisible = false
        };

        Context.Skills.AddRange(skill1, skill2);
        Context.UserSkills.AddRange(userSkill1, userSkill2);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/skill/users/{_testUser.Id}?visibleOnly=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userSkills = await response.Content.ReadFromJsonAsync<List<UserSkillDto>>(TestJsonOptions.Default);
        Assert.NotNull(userSkills);
        Assert.Single(userSkills);
        Assert.Equal("C#", userSkills[0].Skill.Name);
    }

    [Fact]
    [FastTest]
    public async Task GetSkillCategories_ReturnsAllCategories()
    {
        // Arrange
        AuthenticateAs(_testUser);
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "C#", Category = "Programming" });
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "Java", Category = "Programming" });
        await PostWithCsrfAsync("/api/skill", new CreateSkillDto { Name = "Photoshop", Category = "Design" });

        // Act
        var response = await Client.GetAsync("/api/skill/categories");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var categories = await response.Content.ReadFromJsonAsync<List<SkillCategoryDto>>(TestJsonOptions.Default);
        Assert.NotNull(categories);
        Assert.Contains(categories, c => c.Name == "Programming" && c.SkillCount == 2);
        Assert.Contains(categories, c => c.Name == "Design" && c.SkillCount == 1);
    }

    [Fact(Skip = "Requires Admin role in database - role-based authorization checks database, not just claims")]
    [SlowTest]
    public async Task InitializeSystemSkills_CreatesSystemSkills()
    {
        // Arrange
        AuthenticateAs(_testUser, roles: new[] { "Admin" });

        // Act
        var response = await Client.PostAsync("/api/skill/initialize-system", null);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var getResponse = await Client.GetAsync("/api/skill?take=100");
        var content = await getResponse.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SkillSearchResultDto>(content, TestJsonOptions.Default);

        Assert.NotNull(result);
        Assert.True(result.TotalCount > 0);
        Assert.Contains(result.Skills, s => s.Name == "C#");
        Assert.Contains(result.Skills, s => s.IsSystemManaged);
    }

    [Fact]
    [FastTest]
    public async Task DeleteUserSkill_WithValidId_ReturnsNoContent()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill = new Skill { Name = "Test Skill", Category = "Testing" };
        var userSkill = new UserSkill
        {
            UserId = _testUser.Id,
            SkillId = skill.Id,
            Proficiency = SkillProficiency.Beginner
        };

        Context.Skills.Add(skill);
        Context.UserSkills.Add(userSkill);
        await Context.SaveChangesAsync();

        // Act
        var response = await DeleteWithCsrfAsync($"/api/skill/users/{_testUser.Id}/{userSkill.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await Client.GetAsync($"/api/skill/users/{_testUser.Id}");
        var userSkills = await getResponse.Content.ReadFromJsonAsync<List<UserSkillDto>>(TestJsonOptions.Default);
        Assert.NotNull(userSkills);
        Assert.Empty(userSkills);
    }

}

public class SkillSearchResultDto
{
    public List<SkillDto> Skills { get; set; } = new();
    public int TotalCount { get; set; }
}