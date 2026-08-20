using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Core.Entities;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Infrastructure;

[UnitTest]
public class LightweightIntegrationTest : LightweightIntegrationTestBase
{
    [Fact]
    public void Database_ShouldBeCreated()
    {
        // Arrange & Act
        var result = Context.Database.CanConnect();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ServiceProvider_ShouldBeConfigured()
    {
        // Arrange & Act
        var emailService = ServiceScope.ServiceProvider.GetService<SkillLedger.Core.Interfaces.IEmailService>();

        // Assert
        Assert.NotNull(emailService);
    }

    [Fact]
    public async Task Database_ShouldAllowBasicOperations()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "test@example.com",
            NormalizedEmail = "TEST@EXAMPLE.COM",
            NormalizedUserName = "TEST@EXAMPLE.COM"
        };

        // Act
        Context.Users.Add(user);
        await Context.SaveChangesAsync();

        var savedUser = Context.Users.FirstOrDefault(u => u.Email == "test@example.com");

        // Assert
        Assert.NotNull(savedUser);
        Assert.Equal(user.Id, savedUser.Id);
        Assert.Equal("test@example.com", savedUser.Email);
    }
}