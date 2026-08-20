using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Interfaces;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Infrastructure.Services.TestData;
using System.Diagnostics;

namespace SkillLedger.Tests.Tools.DatabaseSeeder;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // Parse command-line arguments
            var options = ParseArguments(args);

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddEnvironmentVariables()
                .Build();

            // Build service provider
            var host = CreateHostBuilder(args, configuration).Build();
            var serviceProvider = host.Services;

            // Get the seeder service
            var seeder = serviceProvider.GetRequiredService<ITestDataSeederService>();

            Console.WriteLine();
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("🌱 SkillLedger Database Test Data Seeder");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            var stopwatch = Stopwatch.StartNew();

            // Execute based on options
            if (options.CleanOnly)
            {
                Console.WriteLine("🧹 Cleaning test data...");
                await seeder.CleanTestDataAsync();
                Console.WriteLine("✅ Test data cleaned successfully!");
            }
            else if (options.OnlyEntities.Any())
            {
                Console.WriteLine($"🎯 Seeding specific entities: {string.Join(", ", options.OnlyEntities)}");
                await SeedSpecificEntities(seeder, options.OnlyEntities, options.Verbose);
            }
            else
            {
                Console.WriteLine("🌱 Seeding full test database...");
                var result = await seeder.SeedAsync(fullSeed: true);

                stopwatch.Stop();

                Console.WriteLine();
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine("✅ Database seeded successfully!");
                Console.WriteLine("═══════════════════════════════════════════════════════════");
                Console.WriteLine();
                Console.WriteLine("📊 Summary:");
                Console.WriteLine($"   Users:               {result.UsersCreated}");
                Console.WriteLine($"   Profiles:            {result.ProfilesCreated}");
                Console.WriteLine($"   Projects:            {result.ProjectsCreated}");
                Console.WriteLine($"   Wallets:             {result.WalletsCreated}");
                Console.WriteLine($"   Transactions:        {result.TransactionsCreated}");
                Console.WriteLine($"   Escrow Accounts:     {result.EscrowAccountsCreated}");
                Console.WriteLine($"   Workspaces:          {result.WorkspacesCreated}");
                Console.WriteLine($"   Messages:            {result.MessagesCreated}");
                Console.WriteLine($"   Documents:           {result.DocumentsCreated}");
                Console.WriteLine($"   Reviews:             {result.ReviewsCreated}");
                Console.WriteLine();
                Console.WriteLine($"⏱️  Execution time: {stopwatch.Elapsed.TotalSeconds:F2}s");
                Console.WriteLine();

                if (options.Verbose)
                {
                    Console.WriteLine("📝 Detailed timing:");
                    Console.WriteLine($"   Started:  {result.StartedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"   Completed: {result.CompletedAt:yyyy-MM-dd HH:mm:ss}");
                    Console.WriteLine($"   Duration: {result.ExecutionTimeMs}ms");
                    Console.WriteLine();
                }
            }

            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine();

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine();
            Console.WriteLine("❌ ERROR: Seeding failed!");
            Console.WriteLine($"   {ex.Message}");

            if (ex.InnerException != null)
            {
                Console.WriteLine($"   Inner: {ex.InnerException.Message}");
            }

            Console.WriteLine();
            Console.WriteLine("Stack trace:");
            Console.WriteLine(ex.StackTrace);
            Console.WriteLine();

            return 1;
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args, IConfiguration configuration)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Database context
                var connectionString = configuration.GetConnectionString("DefaultConnection");
                services.AddDbContext<SkillLedgerDbContext>(options =>
                    options.UseNpgsql(connectionString));

                // ASP.NET Identity services
                services.AddIdentityCore<User>(options =>
                {
                    options.Password.RequireDigit = true;
                    options.Password.RequireLowercase = true;
                    options.Password.RequireUppercase = true;
                    options.Password.RequireNonAlphanumeric = true;
                    options.Password.RequiredLength = 12;
                })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<SkillLedgerDbContext>();

                // Memory caching (required by AzureKeyVaultService)
                services.AddMemoryCache();
                services.AddDistributedMemoryCache();

                // Core services
                services.AddScoped<IEncryptionService, EncryptionService>();
                services.AddScoped<IAzureKeyVaultService, AzureKeyVaultService>();
                services.AddScoped<IAuditLogService, AuditLogService>();

                // Test data factories
                services.AddScoped<UserTestDataFactory>();
                services.AddScoped<ProjectTestDataFactory>();
                services.AddScoped<CreditTestDataFactory>();
                services.AddScoped<WorkspaceTestDataFactory>();

                // Test data seeder service
                services.AddScoped<ITestDataSeederService, TestDataSeederService>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
    }

    static async Task SeedSpecificEntities(ITestDataSeederService seeder, List<string> entities, bool verbose)
    {
        foreach (var entity in entities)
        {
            Console.WriteLine($"🎯 Seeding {entity}...");

            switch (entity.ToLowerInvariant())
            {
                case "users":
                    var userResult = await seeder.SeedUsersAsync();
                    Console.WriteLine($"✅ Created {userResult.UsersCreated} users");
                    break;

                case "projects":
                    var projectResult = await seeder.SeedProjectsAsync();
                    Console.WriteLine($"✅ Created {projectResult.ProjectsCreated} projects");
                    break;

                case "financial":
                case "finance":
                    var financeResult = await seeder.SeedFinancialDataAsync();
                    Console.WriteLine($"✅ Created {financeResult.WalletsCreated} wallets, {financeResult.TransactionsCreated} transactions");
                    break;

                case "collaboration":
                case "workspaces":
                    var collabResult = await seeder.SeedCollaborationDataAsync();
                    Console.WriteLine($"✅ Created {collabResult.WorkspacesCreated} workspaces, {collabResult.MessagesCreated} messages");
                    break;

                case "reputation":
                case "reviews":
                    var reputationResult = await seeder.SeedReputationDataAsync();
                    Console.WriteLine($"✅ Created {reputationResult.ReviewsCreated} reviews");
                    break;

                default:
                    Console.WriteLine($"⚠️  Unknown entity type: {entity}");
                    break;
            }
        }
    }

    static SeederOptions ParseArguments(string[] args)
    {
        var options = new SeederOptions();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--clean":
                    options.CleanOnly = true;
                    break;

                case "--verbose":
                case "-v":
                    options.Verbose = true;
                    break;

                case "--only":
                    if (i + 1 < args.Length)
                    {
                        var entities = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries);
                        options.OnlyEntities.AddRange(entities.Select(e => e.Trim()));
                        i++; // Skip next arg
                    }
                    break;

                case "--help":
                case "-h":
                    PrintHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    static void PrintHelp()
    {
        Console.WriteLine();
        Console.WriteLine("SkillLedger Database Test Data Seeder");
        Console.WriteLine();
        Console.WriteLine("USAGE:");
        Console.WriteLine("  dotnet run                                  Seed entire database");
        Console.WriteLine("  dotnet run -- --clean                       Clean all test data");
        Console.WriteLine("  dotnet run -- --only users,projects         Seed specific entities");
        Console.WriteLine("  dotnet run -- --verbose                     Verbose output");
        Console.WriteLine("  dotnet run -- --help                        Show this help");
        Console.WriteLine();
        Console.WriteLine("ENTITY OPTIONS (for --only):");
        Console.WriteLine("  users          20 test user personas");
        Console.WriteLine("  projects       30 test projects");
        Console.WriteLine("  financial      Credit wallets and transactions");
        Console.WriteLine("  collaboration  Workspaces and messages");
        Console.WriteLine("  reputation     Project reviews");
        Console.WriteLine();
        Console.WriteLine("EXAMPLES:");
        Console.WriteLine("  dotnet run -- --only users,projects");
        Console.WriteLine("  dotnet run -- --clean --verbose");
        Console.WriteLine();
    }

    class SeederOptions
    {
        public bool CleanOnly { get; set; }
        public bool Verbose { get; set; }
        public List<string> OnlyEntities { get; set; } = new();
    }
}
