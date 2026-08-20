using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Core.Entities;
using SkillLedger.Tests.Mocks;
using Microsoft.EntityFrameworkCore.Storage;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Lightweight integration test base that avoids WebApplicationFactory
/// Uses minimal service configuration to prevent memory exhaustion
/// </summary>
public abstract class LightweightIntegrationTestBase : IDisposable, IAsyncDisposable
{
    protected readonly ServiceProvider ServiceProvider;
    protected readonly SkillLedgerDbContext Context;
    protected readonly IServiceScope ServiceScope;
    private readonly string _databaseName;
    private static readonly InMemoryDatabaseRoot SharedDatabaseRoot = new InMemoryDatabaseRoot();

    protected LightweightIntegrationTestBase()
    {
        _databaseName = $"LightweightTestDb_{Guid.NewGuid():N}";

        var services = new ServiceCollection();
        ConfigureServices(services);

        ServiceProvider = services.BuildServiceProvider();
        ServiceScope = ServiceProvider.CreateScope();
        Context = ServiceScope.ServiceProvider.GetRequiredService<SkillLedgerDbContext>();

        InitializeDatabase();
    }

    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Configure minimal logging
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Configure in-memory database
        services.AddDbContext<SkillLedgerDbContext>(options =>
        {
            options.UseInMemoryDatabase(_databaseName, databaseRoot: SharedDatabaseRoot);
            options.EnableSensitiveDataLogging(false);
            options.EnableServiceProviderCaching(false);
        });

        // Add minimal required services
        services.AddSingleton<SkillLedger.Core.Interfaces.IEmailService, MockEmailService>();

        // Add memory cache
        services.AddMemoryCache();

        // Configure basic configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "SkillLedger-Test",
                ["Jwt:Audience"] = "SkillLedger-Test-Users",
                ["Jwt:AccessTokenLifetimeMinutes"] = "60",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Jwt:PrivateKey"] = "test-private-key",
                ["Jwt:PublicKey"] = "test-public-key"
            });

        services.AddSingleton<IConfiguration>(configurationBuilder.Build());
    }

    private void InitializeDatabase()
    {
        try
        {
            Context.Database.EnsureCreated();

            // Seed minimal test data if needed
            SeedTestData();

            Context.SaveChanges();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization warning: {ex.Message}");
        }
    }

    protected virtual void SeedTestData()
    {
        // Override in derived classes to add specific test data
    }

    protected void CleanDatabase()
    {
        try
        {
            // Remove all test data but keep schema
            Context.RemoveRange(Context.Users);
            Context.SaveChanges();

            // Re-seed if needed
            SeedTestData();
            Context.SaveChanges();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database cleanup warning: {ex.Message}");
        }
    }

    public virtual void Dispose()
    {
        try
        {
            Context?.Dispose();
            ServiceScope?.Dispose();
            ServiceProvider?.Dispose();
        }
        catch (Exception)
        {
            // Ignore disposal errors
        }
    }

    public virtual async ValueTask DisposeAsync()
    {
        try
        {
            if (Context != null)
                await Context.DisposeAsync();

            ServiceScope?.Dispose();

            if (ServiceProvider != null)
                await ServiceProvider.DisposeAsync();
        }
        catch (Exception)
        {
            // Ignore disposal errors
        }
    }
}