using SkillLedger.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Moq;

namespace SkillLedger.Tests.Core.Services;

[UnitTest]
[CoreTest]
public class ProfileServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SkillLedgerDbContext _context;
    private readonly IProfileService _profileService;

    public ProfileServiceTests()
    {
        var services = new ServiceCollection();

        // Add in-memory database
        services.AddDbContext<SkillLedgerDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        // Add mock services
        services.AddSingleton<IAuditLogService, MockAuditLogService>();
        services.AddSingleton<ICacheService, MockCacheService>();
        services.AddSingleton<IFileStorageService, MockFileStorageService>();

        // Add HttpContextAccessor mock
        var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        var mockHttpContext = new Mock<HttpContext>();
        var mockRequest = new Mock<HttpRequest>();
        mockRequest.Setup(r => r.Headers).Returns(new HeaderDictionary());
        mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);
        mockHttpContext.Setup(c => c.Connection.RemoteIpAddress).Returns(System.Net.IPAddress.Parse("127.0.0.1"));
        mockHttpContextAccessor.Setup(a => a.HttpContext).Returns(mockHttpContext.Object);
        services.AddSingleton(mockHttpContextAccessor.Object);

        // Add logging
        services.AddLogging(builder => builder.AddConsole());

        // Add profile service
        services.AddScoped<IProfileService, ProfileService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<SkillLedgerDbContext>();
        _profileService = _serviceProvider.GetRequiredService<IProfileService>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }

    [Fact]
    public async Task CreateProfileAsync_WithValidData_ShouldCreateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "test@example.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe",
            Title = "Software Engineer",
            Company = "Tech Corp",
            IsPublic = true
        };

        // Act
        var result = await _profileService.CreateProfileAsync(userId, createDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Profile);
        Assert.Equal("John", result.Profile.FirstName);
        Assert.Equal("Doe", result.Profile.LastName);
        Assert.Equal("Software Engineer", result.Profile.Title);
        Assert.Equal("Tech Corp", result.Profile.Company);
        Assert.True(result.Profile.IsPublic);
        Assert.True(result.Profile.IsComplete); // Should be complete with first name, last name, and title
    }

    [Fact]
    public async Task CreateProfileAsync_ForNonExistentUser_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _profileService.CreateProfileAsync(userId, createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("User not found", result.Message);
    }

    [Fact]
    public async Task CreateProfileAsync_WhenProfileExists_ShouldFail()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "test@example.com" };
        var existingProfile = new Profile { UserId = userId };

        _context.Users.Add(user);
        _context.Profiles.Add(existingProfile);
        await _context.SaveChangesAsync();

        var createDto = new CreateProfileDto
        {
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = await _profileService.CreateProfileAsync(userId, createDto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Profile already exists for this user", result.Message);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidData_ShouldUpdateProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User { Id = userId, Email = "test@example.com", UserName = "test@example.com" };
        var profile = new Profile
        {
            UserId = userId,
            FirstName = "Old",
            LastName = "Name",
            Title = "Old Title"
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateProfileDto
        {
            FirstName = "New",
            LastName = "Name",
            Title = "New Title",
            Company = "New Company"
        };

        // Act
        var result = await _profileService.UpdateProfileAsync(userId, updateDto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Profile);
        Assert.Equal("New", result.Profile.FirstName);
        Assert.Equal("New Title", result.Profile.Title);
        Assert.Equal("New Company", result.Profile.Company);
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_WithPublicProfile_ShouldReturnProfile()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();

        var user = new User { Id = userId, Email = "john@example.com", UserName = "john@example.com" };
        var profile = new Profile
        {
            UserId = userId,
            FirstName = "John",
            IsPublic = true,
            Visibility = ProfileVisibility.Public
        };

        _context.Users.Add(user);
        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(userId, requestingUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John", result.FirstName);
    }

    [Fact]
    public async Task GetProfileByUserIdAsync_WithPrivateProfileFromDifferentUser_ShouldReturnNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var requestingUserId = Guid.NewGuid();
        var profile = new Profile
        {
            UserId = userId,
            FirstName = "John",
            IsPublic = false,
            Visibility = ProfileVisibility.Private
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.GetProfileByUserIdAsync(userId, requestingUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task HasCompleteProfileAsync_WithCompleteProfile_ShouldReturnTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var profile = new Profile
        {
            UserId = userId,
            FirstName = "John",
            LastName = "Doe",
            Title = "Software Engineer",
            IsComplete = true
        };

        _context.Profiles.Add(profile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.HasCompleteProfileAsync(userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetPublicProfilesAsync_ShouldReturnOnlyPublicProfiles()
    {
        // Arrange
        var publicProfile = new Profile
        {
            UserId = Guid.NewGuid(),
            FirstName = "Public",
            LastName = "User",
            IsPublic = true,
            Visibility = ProfileVisibility.Public
        };
        var privateProfile = new Profile
        {
            UserId = Guid.NewGuid(),
            FirstName = "Private",
            LastName = "User",
            IsPublic = false,
            Visibility = ProfileVisibility.Private
        };

        _context.Profiles.AddRange(publicProfile, privateProfile);
        await _context.SaveChangesAsync();

        // Act
        var result = await _profileService.GetPublicProfilesAsync();

        // Assert
        Assert.Single(result);
        Assert.Equal("Public", result[0].FirstName);
    }
}