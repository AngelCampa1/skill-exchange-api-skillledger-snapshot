using SkillLedger.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;

namespace SkillLedger.Tests.Integration;

[Collection("Integration Other")]
[IntegrationTest]
[CoreTest]
public class ProfileIntegrationTests : IntegrationTestBase
{
    private readonly IProfileService _profileService;
    private readonly IUserService _userService;

    public ProfileIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
        // CRITICAL FIX: Get services from ServiceScope instead of Factory.Services
        // to ensure they share the same DbContext as the test base
        _profileService = ServiceScope.ServiceProvider.GetRequiredService<IProfileService>();
        _userService = ServiceScope.ServiceProvider.GetRequiredService<IUserService>();
    }


    [Fact]
    [FastTest]
    public async Task CreateProfile_WithValidData_ShouldCreateProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createProfileDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Software Engineer",
            Company = "Tech Corp",
            IsPublic = true
        };

        // Act
        var result = await _profileService.CreateProfileAsync(user.Id, createProfileDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Profile);
        Assert.Equal("John", result.Profile.FirstName);
        Assert.Equal("Doe", result.Profile.LastName);
        Assert.Equal("Software Engineer", result.Profile.Title);
        Assert.Equal("Tech Corp", result.Profile.Company);
        Assert.True(result.Profile.IsPublic);
        Assert.True(result.Profile.IsComplete);
    }

    [Fact]
    [FastTest]
    public async Task UpdateProfile_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Developer"
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        var updateDto = new UpdateProfileDto
        {
            FirstName = "Jane",
            LastName = "Smith",
            Title = "Senior Developer",
            Company = "New Company"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(user.Id, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Profile);
        Assert.Equal("Jane", result.Profile.FirstName);
        Assert.Equal("Smith", result.Profile.LastName);
        Assert.Equal("Senior Developer", result.Profile.Title);
        Assert.Equal("New Company", result.Profile.Company);
    }

    [Fact]
    [FastTest]
    public async Task GetProfile_ExistingProfile_ShouldReturnProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Developer",
            IsPublic = true
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("Developer", result.Title);
        Assert.True(result.IsPublic);
    }

    [Fact]
    [FastTest]
    public async Task GetMyProfile_ExistingProfile_ShouldReturnOwnProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Developer",
            IsPublic = false // Private profile
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        // Act
        var result = await _profileService.GetMyProfileAsync(user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe", result.LastName);
        Assert.Equal("Developer", result.Title);
        Assert.False(result.IsPublic); // Should be able to see own private profile
    }

    [Fact]
    [SecurityTest]
    public async Task GetPublicProfiles_ShouldReturnOnlyPublicProfiles()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("user1@test.com");
        var user2 = await CreateTestUserAsync("user2@test.com");

        // Create public profile
        await _profileService.CreateProfileAsync(user1.Id, new CreateProfileDto
        {
            FirstName = "Public",
            LastName = "User",
            Title = "Developer",
            IsPublic = true
        });

        // Create private profile
        await _profileService.CreateProfileAsync(user2.Id, new CreateProfileDto
        {
            FirstName = "Private",
            LastName = "User",
            Title = "Developer",
            IsPublic = false
        });

        // Act
        var result = await _profileService.GetPublicProfilesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Public", result[0].FirstName);
        Assert.True(result[0].IsPublic);
    }

    [Fact]
    [FastTest]
    public async Task GetPublicProfiles_WithSearchTerm_ShouldFilterResults()
    {
        // Arrange
        var user1 = await CreateTestUserAsync("dev@test.com");
        var user2 = await CreateTestUserAsync("designer@test.com");

        await _profileService.CreateProfileAsync(user1.Id, new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Developer",
            Title = "Software Engineer",
            IsPublic = true
        });

        await _profileService.CreateProfileAsync(user2.Id, new CreateProfileDto
        {
            FirstName = "Jane",
            LastName = "Designer",
            Title = "UI/UX Designer",
            IsPublic = true
        });

        // Act
        var result = await _profileService.GetPublicProfilesAsync("Developer");

        // Assert
        Assert.Single(result);
        Assert.Equal("John", result[0].FirstName);
        Assert.Contains("Developer", result[0].LastName);
    }

    [Fact]
    [SlowTest]
    public async Task GetPublicProfiles_WithPagination_ShouldRespectLimits()
    {
        // Arrange
        var users = new List<User>();
        for (int i = 0; i < 5; i++)
        {
            var user = await CreateTestUserAsync($"user{i}@test.com");
            users.Add(user);

            await _profileService.CreateProfileAsync(user.Id, new CreateProfileDto
            {
                FirstName = $"User{i}",
                LastName = "Test",
                Title = "Developer",
                IsPublic = true
            });
        }

        // Act
        var firstPage = await _profileService.GetPublicProfilesAsync(null, 0, 2);
        var secondPage = await _profileService.GetPublicProfilesAsync(null, 2, 2);

        // Assert
        Assert.Equal(2, firstPage.Count);
        Assert.Equal(2, secondPage.Count);

        // Ensure different results
        Assert.NotEqual(firstPage[0].FirstName, secondPage[0].FirstName);
    }

    [Fact]
    [FastTest]
    public async Task DeleteProfile_ExistingProfile_ShouldDeleteProfile()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Developer"
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        // Act
        var deleteResult = await _profileService.DeleteProfileAsync(user.Id);
        var getResult = await _profileService.GetMyProfileAsync(user.Id);

        // Assert
        Assert.True(deleteResult.Success);
        Assert.Null(getResult); // Profile should no longer exist
    }

    [Fact]
    [FastTest]
    public async Task HasCompleteProfile_CompleteProfile_ShouldReturnTrue()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Developer" // All required fields for completeness
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        // Act
        var result = await _profileService.HasCompleteProfileAsync(user.Id);

        // Assert
        Assert.True(result);
    }

    [Fact]
    [FastTest]
    public async Task HasCompleteProfile_IncompleteProfile_ShouldReturnFalse()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var createDto = new CreateProfileDto
        {
            FirstName = "John"
            // Missing LastName and Title - incomplete
        };

        await _profileService.CreateProfileAsync(user.Id, createDto);

        // Act
        var result = await _profileService.HasCompleteProfileAsync(user.Id);

        // Assert
        Assert.False(result);
    }

    private async Task<User> CreateTestUserAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            EmailConfirmed = true,
            TaxCompliant = false,
            PasswordHash = "dummy-hash"
        };

        Context.Users.Add(user);
        await Context.SaveChangesAsync();
        SimpleTestDataSeeder.CreateActiveSubscriptionForUser(Context, user.Id);
        return user;
    }
}