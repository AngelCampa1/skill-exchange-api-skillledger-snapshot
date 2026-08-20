using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration;

/// <summary>
/// Simple test to verify database name propagation between test setup and HTTP requests
/// </summary>
[Collection("Integration Other")]
public class DatabaseNamePropagationTest : IntegrationTestBase
{
    public DatabaseNamePropagationTest(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    [Fact]
    [FastTest]
    public async Task DatabaseName_ShouldBeConsistentAcrossContexts()
    {
        // Arrange
        var testEmail = "propagation-test@example.com";

        // Act - Create user through test infrastructure
        var testUser = await CreateTestUserAsync(testEmail, "TestPassword123!", emailVerified: true);

        // Debug: Check database names at different points
        var testContextDatabaseName = DatabaseContextValidationHelper.GetActualDatabaseName(ServiceScope.ServiceProvider);
        Console.WriteLine($"Test context database name: {testContextDatabaseName}");
        Console.WriteLine($"Expected database name: {DatabaseName}");

        // Check if user exists in test context
        var userInTestContext = await Context.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
        Console.WriteLine($"User found in test context: {userInTestContext != null}");

        // Create a new service scope (simulating HTTP request scope)
        using var httpScope = Factory.Services.CreateScope();
        var httpContext = httpScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

        var httpContextDatabaseName = DatabaseContextValidationHelper.GetActualDatabaseName(httpScope.ServiceProvider);
        Console.WriteLine($"HTTP context database name: {httpContextDatabaseName}");

        // Check if user exists in HTTP context
        var userInHttpContext = await httpContext.Users.FirstOrDefaultAsync(u => u.Email == testEmail);
        Console.WriteLine($"User found in HTTP context: {userInHttpContext != null}");

        // Assert
        Assert.Equal(DatabaseName, testContextDatabaseName);
        Assert.Equal(DatabaseName, httpContextDatabaseName);
        Assert.NotNull(userInTestContext);
        Assert.NotNull(userInHttpContext);
        Assert.Equal(userInTestContext.Id, userInHttpContext.Id);
    }
}