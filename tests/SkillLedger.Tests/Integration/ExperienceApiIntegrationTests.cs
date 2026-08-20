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
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration;

[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class ExperienceApiIntegrationTests : IntegrationTestBase
{
    private User _testUser = null!;

    public ExperienceApiIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();
        await SeedTestDataAsync();
    }

    private async Task SeedTestDataAsync()
    {
        // Use standard test user that already exists - this user will be used for authentication
        var standardUsers = SimpleTestDataSeeder.GetStandardUsers(Context);
        Console.WriteLine($"Found {standardUsers.Count} standard users");

        if (standardUsers.Any())
        {
            _testUser = standardUsers.First();
            Console.WriteLine($"Using standard user: ID={_testUser.Id}, Email={_testUser.Email}");
        }
        else
        {
            // Fallback: create a simple user record (this won't work for auth but will allow basic testing)
            _testUser = new User
            {
                Id = Guid.NewGuid(),
                Email = "testuser@example.com",
                UserName = "testuser@example.com",
                Status = UserStatus.Active
            };

            Context.Users.Add(_testUser);
            await Context.SaveChangesAsync();
            Console.WriteLine($"Created fallback user: ID={_testUser.Id}, Email={_testUser.Email}");
        }
    }

    [Fact]
    [FastTest]
    public async Task CreateExperience_WithValidData_ReturnsCreated()
    {
        // Arrange - Add authentication header
        AuthenticateAs(_testUser);

        var createExperienceDto = new CreateExperienceDto
        {
            Title = "Senior Software Developer",
            Organization = "Tech Corp",
            Description = "Led development of web applications",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2023, 12, 31),
            IsVisible = true
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", createExperienceDto);

        // Debug: Check actual response
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Experience creation failed: {response.StatusCode}");
            Console.WriteLine($"Error content: {errorContent}");
        }

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var experience = await response.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);
        Assert.NotNull(experience);
        Assert.Equal("Senior Software Developer", experience.Title);
        Assert.Equal("Tech Corp", experience.Organization);
        Assert.Equal(_testUser.Id, experience.UserId);
    }

    [Fact]
    [FastTest]
    public async Task GetUserExperiences_ReturnsTimelineOrderedExperiences()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var experience1 = new CreateExperienceDto
        {
            Title = "Junior Developer",
            Organization = "StartupCorp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2018, 1, 1),
            EndDate = new DateTime(2019, 12, 31),
            IsVisible = true
        };

        var experience2 = new CreateExperienceDto
        {
            Title = "Senior Developer",
            Organization = "BigCorp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = null, // Current position
            IsVisible = true
        };

        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", experience1);
        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", experience2);

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experiences = await response.Content.ReadFromJsonAsync<List<ExperienceDto>>(TestJsonOptions.Default);
        Assert.NotNull(experiences);
        Assert.Equal(2, experiences.Count);

        // Should be ordered by start date descending (most recent first)
        Assert.Equal("Senior Developer", experiences[0].Title);
        Assert.Equal("Junior Developer", experiences[1].Title);
    }

    [Fact]
    [FastTest]
    public async Task UpdateExperience_WithValidData_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var createDto = new CreateExperienceDto
        {
            Title = "Developer",
            Organization = "Tech Corp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            IsVisible = true
        };

        var createResponse = await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", createDto);

        // Debug: Check actual response
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Experience creation failed: {createResponse.StatusCode}");
            Console.WriteLine($"Error content: {errorContent}");
        }

        // Assert creation was successful
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdExperience = await createResponse.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);

        var updateDto = new UpdateExperienceDto
        {
            Title = "Senior Developer",
            Description = "Updated description with more responsibilities",
            EndDate = new DateTime(2023, 12, 31)
        };

        // Act
        var response = await Client.PutAsJsonAsync($"/api/experience/{_testUser.Id}/{createdExperience!.Id}", updateDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updatedExperience = await response.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);
        Assert.NotNull(updatedExperience);
        Assert.Equal("Senior Developer", updatedExperience.Title);
        Assert.Equal("Updated description with more responsibilities", updatedExperience.Description);
        Assert.Equal(new DateTime(2023, 12, 31), updatedExperience.EndDate);
    }

    [Fact]
    [FastTest]
    public async Task GetExperienceById_WithSkills_ReturnsExperienceWithSkills()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill = new Skill { Name = "C#", Category = "Programming" };
        Context.Skills.Add(skill);

        var experience = new Experience
        {
            UserId = _testUser.Id,
            Title = "Developer",
            Organization = "Tech Corp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            IsVisible = true
        };
        Context.Experiences.Add(experience);

        var experienceSkill = new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill.Id,
            Notes = "Used C# extensively in this role"
        };
        Context.ExperienceSkills.Add(experienceSkill);
        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}/{experience.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experienceDto = await response.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);
        Assert.NotNull(experienceDto);
        Assert.Single(experienceDto.Skills);
        Assert.Equal("C#", experienceDto.Skills[0].Name);
    }

    [Fact]
    [FastTest]
    public async Task GetExperiencesByType_WithValidType_ReturnsFilteredExperiences()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var workExperience = new CreateExperienceDto
        {
            Title = "Software Developer",
            Organization = "Tech Corp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            IsVisible = true
        };

        var educationExperience = new CreateExperienceDto
        {
            Title = "Computer Science Degree",
            Organization = "University",
            Type = ExperienceType.Education,
            StartDate = new DateTime(2016, 9, 1),
            EndDate = new DateTime(2020, 5, 1),
            IsVisible = true
        };

        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", workExperience);
        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", educationExperience);

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}?type={ExperienceType.Work}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experiences = await response.Content.ReadFromJsonAsync<List<ExperienceDto>>(TestJsonOptions.Default);
        Assert.NotNull(experiences);
        Assert.Single(experiences);
        Assert.Equal("Software Developer", experiences[0].Title);
        Assert.Equal(ExperienceType.Work, experiences[0].Type);
    }

    [Fact]
    [FastTest]
    public async Task GetExperienceById_WithValidId_ReturnsExperience()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var createDto = new CreateExperienceDto
        {
            Title = "Project Manager",
            Organization = "Consulting Corp",
            Description = "Managed multiple client projects",
            Type = ExperienceType.Project,
            StartDate = new DateTime(2021, 3, 1),
            EndDate = new DateTime(2022, 2, 28),
            IsVisible = true
        };

        var createResponse = await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", createDto);
        var createdExperience = await createResponse.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}/{createdExperience!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experience = await response.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);
        Assert.NotNull(experience);
        Assert.Equal("Project Manager", experience.Title);
        Assert.Equal("Consulting Corp", experience.Organization);
        Assert.Equal("Managed multiple client projects", experience.Description);
    }

    [Fact]
    [FastTest]
    public async Task DeleteExperience_WithValidId_ReturnsNoContent()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var createDto = new CreateExperienceDto
        {
            Title = "Intern",
            Organization = "Startup Inc",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2019, 6, 1),
            EndDate = new DateTime(2019, 8, 31),
            IsVisible = true
        };

        var createResponse = await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", createDto);
        var createdExperience = await createResponse.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default);

        // Act
        var response = await Client.DeleteAsync($"/api/experience/{_testUser.Id}/{createdExperience!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the experience was deleted
        var getResponse = await Client.GetAsync($"/api/experience/{_testUser.Id}");
        var experiences = await getResponse.Content.ReadFromJsonAsync<List<ExperienceDto>>();

        Assert.NotNull(experiences);
        Assert.Empty(experiences);
    }

    [Fact]
    [FastTest]
    public async Task GetMultipleExperiences_ReturnsAllExperiences()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var experience1 = new CreateExperienceDto
        {
            Title = "Software Engineer",
            Organization = "Tech Corp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2020, 1, 1),
            IsVisible = true
        };

        var experience2 = new CreateExperienceDto
        {
            Title = "Computer Science Degree",
            Organization = "University",
            Type = ExperienceType.Education,
            StartDate = new DateTime(2016, 9, 1),
            EndDate = new DateTime(2020, 5, 1),
            IsVisible = true
        };

        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", experience1);
        await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", experience2);

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experiences = await response.Content.ReadFromJsonAsync<List<ExperienceDto>>(TestJsonOptions.Default);
        Assert.NotNull(experiences);
        Assert.Equal(2, experiences.Count);
    }

    [Fact]
    [SlowTest]
    public async Task SearchExperiencesBySkill_WithValidSkillName_ReturnsMatchingExperiences()
    {
        // Arrange
        AuthenticateAs(_testUser);
        var skill = new Skill { Name = "Python", Category = "Programming" };
        Context.Skills.Add(skill);

        var experience = new Experience
        {
            UserId = _testUser.Id,
            Title = "Data Scientist",
            Organization = "Analytics Corp",
            Type = ExperienceType.Work,
            StartDate = new DateTime(2021, 1, 1),
            IsVisible = true
        };
        Context.Experiences.Add(experience);

        var experienceSkill = new ExperienceSkill
        {
            ExperienceId = experience.Id,
            SkillId = skill.Id,
            Notes = "Used Python for data analysis"
        };
        Context.ExperienceSkills.Add(experienceSkill);

        await Context.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/experience/{_testUser.Id}?skillName=Python");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var experiences = await response.Content.ReadFromJsonAsync<List<ExperienceDto>>(TestJsonOptions.Default);
        Assert.NotNull(experiences);
        Assert.Single(experiences);
        Assert.Equal("Data Scientist", experiences[0].Title);
        Assert.Single(experiences[0].Skills);
        Assert.Equal("Python", experiences[0].Skills[0].Name);
    }

    private async Task<ExperienceDto> CreateTestExperience(string title, int displayOrder)
    {
        var createDto = new CreateExperienceDto
        {
            Title = title,
            Organization = "Test Corp",
            Type = ExperienceType.Work,
            StartDate = DateTime.Now.AddYears(-displayOrder),
            IsVisible = true
        };

        var response = await Client.PostAsJsonAsync($"/api/experience/{_testUser.Id}", createDto);
        return (await response.Content.ReadFromJsonAsync<ExperienceDto>(TestJsonOptions.Default))!;
    }

}