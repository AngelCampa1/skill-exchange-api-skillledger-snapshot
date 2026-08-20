using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Mocks;

namespace SkillLedger.Tests.Fixtures;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Use Testing environment
            context.HostingEnvironment.EnvironmentName = "Testing";

            // Add test configuration
            var testConfig = new Dictionary<string, string>
            {
                ["Jwt:Issuer"] = "https://localhost:7001",
                ["Jwt:Audience"] = "https://localhost:7001",
                ["Jwt:AccessTokenLifetimeMinutes"] = "15",
                ["Jwt:RefreshTokenLifetimeDays"] = "7",
                ["Jwt:RequireHttpsMetadata"] = "false",
                ["Jwt:ValidateIssuer"] = "true",
                ["Jwt:ValidateAudience"] = "true",
                ["Jwt:ValidateLifetime"] = "true",
                ["Jwt:ValidateIssuerSigningKey"] = "true",
                ["Jwt:ClockSkewMinutes"] = "2",
                ["Jwt:EnableTokenBlacklisting"] = "true",
                ["Jwt:MaxRefreshTokensPerUser"] = "5",
                ["Jwt:AutoCleanupExpiredTokens"] = "true",
                ["Jwt:CleanupIntervalHours"] = "24",
                ["AzureKeyVault:Enabled"] = "false",
                ["AzureKeyVault:VaultUri"] = "",
                ["AzureKeyVault:UseManagedIdentity"] = "false",
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=SkillLedgerTestWeb;Trusted_Connection=true;MultipleActiveResultSets=true",
                ["ConnectionStrings:AzureCommunicationServices"] = "endpoint=https://REPLACE-WITH-YOUR-ACS.communication.azure.com/;accesskey=fake-access-key-for-testing-only",
                ["EmailSettings:FromEmail"] = "noreply@skillledger-test.com",
                ["EmailSettings:FromDisplayName"] = "SkillLedger Test",
                ["RateLimiting:RegistrationPerHour"] = "1000",
                ["RateLimiting:VerificationPerHour"] = "1000",
                ["RateLimiting:LoginAttemptsPerMinute"] = "1000",
                ["RateLimiting:GeneralApiPerMinute"] = "10000"
            };

            config.AddInMemoryCollection(testConfig!);
        });

        builder.ConfigureServices(services =>
        {
            // Replace email service with mock for testing
            var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(SkillLedger.Core.Interfaces.IEmailService));
            if (emailDescriptor != null)
            {
                services.Remove(emailDescriptor);
            }
            services.AddSingleton<SkillLedger.Core.Interfaces.IEmailService, MockEmailService>();

            // Note: We keep the real database and audit service for integration tests
            // since we want to test the full stack behavior
        });

        builder.UseEnvironment("Testing");
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        return host;
    }
}