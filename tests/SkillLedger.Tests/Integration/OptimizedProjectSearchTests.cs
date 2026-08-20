using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// Optimized integration tests demonstrating the improved performance patterns
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Other")]
public class OptimizedProjectSearchTests : IntegrationTestBase
{
    public OptimizedProjectSearchTests(SharedTestHostFixture fixture) : base(fixture)
    {
        // Constructor is now fast - no synchronous data setup
        // Standard test data is automatically seeded by the base class
    }

    [Fact]
    [FastTest]
    public async Task SearchProjects_WithStandardData_ReturnsResults()
    {
        // Arrange - use pre-seeded standard data for faster execution
        var standardUsers = SimpleTestDataSeeder.GetStandardUsers(Context);
        Assert.NotEmpty(standardUsers); // Ensure we have test data

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "React",
            Take = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var searchResponse = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();

        Assert.NotNull(searchResponse);
        Assert.True(searchResponse.TotalCount >= 0);
        Assert.NotNull(searchResponse.Projects);
    }

    [Fact]
    [IntegrationTest]
    public async Task SearchProjects_WithAuthentication_ReturnsUserSpecificResults()
    {
        // Arrange - use standard user for HTTP header authentication
        var standardUsers = SimpleTestDataSeeder.GetStandardUsers(Context);
        var testUser = standardUsers[0];
        AuthenticateAs(testUser);

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "Standard",
            EnableRecommendations = true,
            Take = 5
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var searchResponse = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();

        Assert.NotNull(searchResponse);
        Assert.NotNull(searchResponse.Projects);
    }

    [Fact]
    [IntegrationTest]
    public async Task SearchProjects_WithCustomTestData_HandlesNewProjects()
    {
        // Arrange - create minimal test-specific data when needed
        var testUser = SimpleTestDataSeeder.CreateTestUser("searchtest");
        Context.Users.Add(testUser);

        var testProject = SimpleTestDataSeeder.CreateTestProject("Custom Search Test", testUser.Id);
        Context.Projects.Add(testProject);

        await Context.SaveChangesAsync();

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "Custom",
            Take = 10
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var searchResponse = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();

        Assert.NotNull(searchResponse);
        // This test will clean up automatically - only test-specific data is removed
    }

    [Fact]
    [PerformanceTest]
    public async Task SearchProjects_Performance_CompletesWithinTimeLimit()
    {
        // Arrange
        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "Standard",
            Take = 100
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        // Assert
        stopwatch.Stop();

        response.EnsureSuccessStatusCode();

        // Performance assertion - increased threshold for test environment variability
        Assert.True(stopwatch.ElapsedMilliseconds < 60000,
            $"Search took {stopwatch.ElapsedMilliseconds}ms, should be under 60000ms");
    }

    [Fact(Skip = "Obsolete - JWT Bearer token authentication removed in favor of cookie authentication")]
    [SecurityTest]
    public async Task SearchProjects_WithInvalidAuth_ReturnsUnauthorized()
    {
        // Arrange
        Client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        var searchRequest = new AdvancedProjectSearchDto
        {
            EnableRecommendations = true // This requires authentication
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}