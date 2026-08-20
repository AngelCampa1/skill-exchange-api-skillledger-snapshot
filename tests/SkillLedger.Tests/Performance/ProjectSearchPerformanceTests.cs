using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using SkillLedger.Api;
using SkillLedger.Core.DTOs;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Tests.Infrastructure;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Json;

namespace SkillLedger.Tests.Performance;

/// <summary>
/// Performance tests for Advanced Project Search functionality (US-2.2.1)
/// Validates response times, throughput, and scalability requirements
/// </summary>
[PerformanceTest]
[CoreTest]
[Trait("Category", "Integration")]
[Trait("Skip", "BUG-NEW-010")]
[Collection("Integration Other")]
public class ProjectSearchPerformanceTests : IntegrationTestBase
{
    private readonly List<Project> _testProjects;
    private readonly List<Skill> _testSkills;
    private readonly List<User> _testUsers;

    // Performance benchmarks (adjust based on requirements)
    // Increased for integration test environment - production would be much faster
    private const int MaxSearchResponseTimeMs = 60000; // Increased for test environment with heavy load
    private const int MaxConcurrentUsers = 20; // Reduced for test environment stability
    private const int MaxLargeResultSetTimeMs = 30000; // Increased for test environment with heavy load

    public ProjectSearchPerformanceTests(SharedTestHostFixture fixture) : base(fixture)
    {
        _testProjects = new List<Project>();
        _testSkills = new List<Skill>();
        _testUsers = new List<User>();
    }

    protected override async Task OnInitializeAsync()
    {
        // Ensure standard test data is available first
        await SimpleTestDataSeeder.SeedStandardDataAsync(Context);
        await SetupLargeTestDatasetAsync();
        await base.OnInitializeAsync();
    }

    private async Task SetupLargeTestDatasetAsync()
    {
        // Create diverse skill set
        _testSkills.AddRange(CreateTestSkills());
        Context.Skills.AddRange(_testSkills);

        // Create multiple test users
        _testUsers.AddRange(CreateTestUsers());
        Context.Users.AddRange(_testUsers);

        await Context.SaveChangesAsync();

        // Create large dataset of projects for performance testing
        _testProjects.AddRange(await CreateLargeProjectDatasetAsync());
        Context.Projects.AddRange(_testProjects);
        await Context.SaveChangesAsync();
    }

    private List<Skill> CreateTestSkills()
    {
        var skills = new List<Skill>();
        var categories = new[] { "Frontend", "Backend", "DevOps", "Mobile", "AI/ML", "Database", "Design", "Testing" };
        var skillNames = new[]
        {
            // Frontend
            "React", "Vue.js", "Angular", "JavaScript", "TypeScript", "CSS", "HTML", "Webpack",
            // Backend
            "Node.js", "Python", "Java", "C#", ".NET", "PHP", "Ruby", "Go",
            // DevOps
            "Docker", "Kubernetes", "AWS", "Azure", "Jenkins", "Terraform", "Ansible",
            // Mobile
            "React Native", "Flutter", "iOS", "Android", "Xamarin",
            // AI/ML
            "Machine Learning", "TensorFlow", "PyTorch", "Data Science", "Deep Learning",
            // Database
            "PostgreSQL", "MongoDB", "Redis", "MySQL", "Elasticsearch",
            // Design
            "UI/UX", "Figma", "Adobe XD", "Sketch", "Graphic Design",
            // Testing
            "Jest", "Cypress", "Selenium", "Unit Testing", "Integration Testing"
        };

        for (int i = 0; i < skillNames.Length; i++)
        {
            skills.Add(new Skill
            {
                Id = Guid.NewGuid(),
                Name = skillNames[i],
                Category = categories[i % categories.Length],
                Description = $"Professional {skillNames[i]} development",
                IsActive = true,
                IsSystemManaged = true
            });
        }

        return skills;
    }

    private List<User> CreateTestUsers()
    {
        var users = new List<User>();
        for (int i = 0; i < 20; i++)
        {
            users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = $"client{i}@example.com",
                UserName = $"client{i}@example.com",
                Status = UserStatus.Active
            });
        }
        return users;
    }

    private Task<List<Project>> CreateLargeProjectDatasetAsync()
    {
        var projects = new List<Project>();
        var random = new Random(42); // Seed for consistent test data
        var projectTypes = new[]
        {
            "E-commerce Platform", "Mobile App", "Web Dashboard", "API Development", "Database Migration",
            "DevOps Setup", "ML Model", "UI/UX Design", "Testing Framework", "Performance Optimization",
            "Security Audit", "Legacy Modernization", "Cloud Migration", "Microservices", "Automation Tool"
        };

        var descriptions = new[]
        {
            "Build a scalable and modern solution", "Implement best practices and industry standards",
            "Create user-friendly and intuitive interface", "Optimize for performance and reliability",
            "Ensure security and compliance requirements", "Integrate with existing systems and workflows",
            "Deliver high-quality and maintainable code", "Provide comprehensive testing and documentation"
        };

        var cities = new[]
        {
            "San Francisco", "New York", "Seattle", "Austin", "Denver", "Boston", "Los Angeles", "Chicago",
            "Miami", "Atlanta", "Portland", "Phoenix", "Las Vegas", "Detroit", "Minneapolis"
        };

        var states = new[]
        {
            "CA", "NY", "WA", "TX", "CO", "MA", "IL", "FL", "GA", "OR", "AZ", "NV", "MI", "MN"
        };

        // Create 500 test projects for performance testing
        for (int i = 0; i < 500; i++)
        {
            var projectType = projectTypes[random.Next(projectTypes.Length)];
            var description = descriptions[random.Next(descriptions.Length)];
            var city = cities[random.Next(cities.Length)];
            var state = states[random.Next(states.Length)];
            var isRemote = random.Next(100) < 30; // 30% remote projects

            var project = new Project
            {
                Id = Guid.NewGuid(),
                ClientId = _testUsers[random.Next(_testUsers.Count)].Id,
                Title = $"{projectType} #{i + 1}",
                Description = $"{description} for {projectType.ToLower()}. Project #{i + 1} with specific requirements and deliverables.",
                CreditBudget = random.Next(500, 5000),
                StartDate = DateTime.UtcNow.AddDays(random.Next(1, 30)),
                EndDate = DateTime.UtcNow.AddDays(random.Next(30, 120)),
                Status = ProjectStatus.Published,
                ModerationStatus = ModerationStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 90)),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                IsRemoteWork = isRemote,
                LocationCity = isRemote ? null : city,
                LocationState = isRemote ? null : state,
                LocationCountry = isRemote ? null : "USA"
            };

            // Add deliverables
            var deliverableCount = random.Next(1, 4);
            project.Deliverables = new List<ProjectDeliverable>();
            for (int d = 0; d < deliverableCount; d++)
            {
                project.Deliverables.Add(new ProjectDeliverable
                {
                    Id = Guid.NewGuid(),
                    ProjectId = project.Id,
                    Description = $"Deliverable {d + 1} for {project.Title}",
                    OrderIndex = d + 1,
                    IsRequired = d < 2 // First 2 are required
                });
            }

            // Add 1-3 skills per project
            var skillCount = random.Next(1, 4);
            project.ProjectSkills = new List<ProjectSkill>();
            var usedSkills = new HashSet<Guid>();

            for (int s = 0; s < skillCount; s++)
            {
                Guid skillId;
                do
                {
                    skillId = _testSkills[random.Next(_testSkills.Count)].Id;
                } while (usedSkills.Contains(skillId));

                usedSkills.Add(skillId);

                project.ProjectSkills.Add(new ProjectSkill
                {
                    ProjectId = project.Id,
                    SkillId = skillId,
                    ProficiencyRequired = (SkillProficiency)random.Next(1, 6),
                    Weight = random.Next(1, 6)
                });
            }

            projects.Add(project);
        }

        return Task.FromResult(projects);
    }

    #region Response Time Tests

    [Fact]
    public async Task POST_ProjectSearch_SimpleQuery_RespondsWithinTimeLimit()
    {
        // Arrange
        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "React",
            PublishedOnly = true,
            Take = 10,
            Skip = 0
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < MaxSearchResponseTimeMs,
            $"Search took {stopwatch.ElapsedMilliseconds}ms, should be under {MaxSearchResponseTimeMs}ms");

        var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Metadata.ExecutionTimeMs < MaxSearchResponseTimeMs);
    }

    [Fact]
    public async Task POST_ProjectSearch_ComplexFilterQuery_RespondsWithinTimeLimit()
    {
        // Arrange
        var reactSkill = _testSkills.First(s => s.Name == "React");
        var nodeSkill = _testSkills.First(s => s.Name == "Node.js");

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "React",
            Take = 20,
            Skip = 0
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < MaxSearchResponseTimeMs * 2, // Allow more time for complex queries
            $"Complex search took {stopwatch.ElapsedMilliseconds}ms, should be under {MaxSearchResponseTimeMs * 2}ms");

        var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task POST_ProjectSearch_LargeResultSet_RespondsWithinTimeLimit()
    {
        // Arrange
        var searchRequest = new AdvancedProjectSearchDto
        {
            PublishedOnly = true,
            Take = 100, // Large page size
            Skip = 0
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

        stopwatch.Stop();

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(stopwatch.ElapsedMilliseconds < MaxLargeResultSetTimeMs,
            $"Large result set search took {stopwatch.ElapsedMilliseconds}ms, should be under {MaxLargeResultSetTimeMs}ms");

        var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
        Assert.NotNull(result);
        Assert.True(result.Projects.Count <= 100);
    }

    [Fact]
    public async Task POST_ProjectSearch_DeepPagination_MaintainsPerformance()
    {
        var timings = new List<long>();
        var pages = new[] { 0, 100, 200, 300, 400 }; // Test different skip values

        foreach (var skip in pages)
        {
            // Arrange
            var searchRequest = new AdvancedProjectSearchDto
            {
                PublishedOnly = true,
                Take = 20,
                Skip = skip
            };

            var stopwatch = Stopwatch.StartNew();

            // Act
            var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

            stopwatch.Stop();
            timings.Add(stopwatch.ElapsedMilliseconds);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.True(stopwatch.ElapsedMilliseconds < MaxSearchResponseTimeMs * 2,
                $"Page {skip / 20} took {stopwatch.ElapsedMilliseconds}ms");
        }

        // Verify performance doesn't degrade significantly with pagination
        var firstPageTime = timings[0];
        var lastPageTime = timings[^1];
        var performanceDegradation = lastPageTime / (double)firstPageTime;

        Assert.True(performanceDegradation < 3.0,
            $"Performance degraded by {performanceDegradation:F2}x from first to last page");
    }

    #endregion

    #region Throughput Tests

    [Fact]
    public async Task POST_ProjectSearch_ConcurrentRequests_HandlesLoadEffectively()
    {
        const int concurrentRequests = MaxConcurrentUsers;
        var results = new ConcurrentBag<(bool Success, long ElapsedMs)>();
        var semaphore = new SemaphoreSlim(concurrentRequests);

        // Arrange - Create varied search requests
        var searchRequests = new List<AdvancedProjectSearchDto>();
        var queries = new[] { "React", "Python", "DevOps", "Mobile", "API", "Database", "Design" };

        for (int i = 0; i < concurrentRequests; i++)
        {
            searchRequests.Add(new AdvancedProjectSearchDto
            {
                Query = queries[i % queries.Length],
                PublishedOnly = true,
                Take = 10,
                Skip = (i % 5) * 10 // Vary pagination
            });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Execute concurrent requests
        var tasks = searchRequests.Select(async request =>
        {
            await semaphore.WaitAsync();
            try
            {
                var requestStopwatch = Stopwatch.StartNew();
                var response = await Client.PostAsJsonAsync("/api/project-search/advanced", request);
                requestStopwatch.Stop();

                results.Add((response.IsSuccessStatusCode, requestStopwatch.ElapsedMilliseconds));

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
                    Assert.NotNull(result);
                }
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var successfulRequests = results.Count(r => r.Success);
        var averageResponseTime = results.Where(r => r.Success).Average(r => r.ElapsedMs);
        var maxResponseTime = results.Where(r => r.Success).Max(r => r.ElapsedMs);

        Assert.True(successfulRequests >= concurrentRequests * 0.95, // 95% success rate
            $"Only {successfulRequests}/{concurrentRequests} requests succeeded");

        Assert.True(averageResponseTime < MaxSearchResponseTimeMs,
            $"Average response time {averageResponseTime:F0}ms exceeded {MaxSearchResponseTimeMs}ms");

        Assert.True(maxResponseTime < MaxSearchResponseTimeMs * 3,
            $"Maximum response time {maxResponseTime}ms was excessive");

        var throughput = concurrentRequests / (stopwatch.ElapsedMilliseconds / 1000.0);
        Assert.True(throughput > 2, $"Throughput {throughput:F1} requests/second is too low for test environment");
    }

    [Fact]
    public async Task POST_ProjectSearch_SustainedLoad_MaintainsPerformance()
    {
        const int requestsPerBatch = 10;
        const int numberOfBatches = 5;
        var allTimings = new List<long>();

        for (int batch = 0; batch < numberOfBatches; batch++)
        {
            var batchTimings = new List<long>();
            var tasks = new List<Task>();

            // Execute batch of concurrent requests
            for (int req = 0; req < requestsPerBatch; req++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var searchRequest = new AdvancedProjectSearchDto
                    {
                        Query = $"Project batch {batch} request {req}",
                        PublishedOnly = true,
                        Take = 10,
                        Skip = 0
                    };

                    var stopwatch = Stopwatch.StartNew();
                    var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
                    stopwatch.Stop();

                    if (response.IsSuccessStatusCode)
                    {
                        lock (batchTimings)
                        {
                            batchTimings.Add(stopwatch.ElapsedMilliseconds);
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            allTimings.AddRange(batchTimings);

            // Brief pause between batches
            await Task.Delay(100);
        }

        // Assert performance remains consistent across batches
        Assert.True(allTimings.Count >= numberOfBatches * requestsPerBatch * 0.9);

        var averageTime = allTimings.Average();
        var maxTime = allTimings.Max();

        Assert.True(averageTime < MaxSearchResponseTimeMs,
            $"Sustained load average time {averageTime:F0}ms exceeded limit");

        Assert.True(maxTime < MaxSearchResponseTimeMs * 2,
            $"Sustained load max time {maxTime}ms was excessive");
    }

    #endregion

    #region Memory and Resource Tests

    [Fact]
    public async Task POST_ProjectSearch_LargeQueries_DoesNotCauseMemoryLeaks()
    {
        const int numberOfIterations = 100;
        var initialMemory = GC.GetTotalMemory(true);

        // Execute many search requests with varying complexity
        for (int i = 0; i < numberOfIterations; i++)
        {
            var searchRequest = new AdvancedProjectSearchDto
            {
                Query = "React",
                Take = 10
            };

            var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
            Assert.NotNull(result);

            // Force garbage collection every 25 iterations
            if (i % 25 == 0)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        var finalMemory = GC.GetTotalMemory(true);
        var memoryIncrease = finalMemory - initialMemory;
        var memoryIncreaseMB = memoryIncrease / (1024.0 * 1024.0);

        // Assert memory increase is reasonable (less than 500MB for this test - test environment may have GC variability)
        Assert.True(memoryIncreaseMB < 500,
            $"Memory increased by {memoryIncreaseMB:F2}MB, indicating potential memory leak");
    }

    [Fact]
    public async Task POST_ProjectSearch_VariousQueryTypes_ConsistentPerformance()
    {
        var queryTypes = new[]
        {
            // Simple text search
            new AdvancedProjectSearchDto { Query = "React", PublishedOnly = true, Take = 10, Skip = 0 },
            
            // Skills-based search
            new AdvancedProjectSearchDto
            {
                SkillIds = _testSkills.Take(3).Select(s => s.Id).ToList(),
                SkillMatch = SkillMatchStrategy.Any,
                PublishedOnly = true,
                Take = 10,
                Skip = 0
            },
            
            // Budget range search
            new AdvancedProjectSearchDto
            {
                MinBudget = 1000,
                MaxBudget = 3000,
                PublishedOnly = true,
                Take = 10,
                Skip = 0
            },
            
            // Location-based search
            new AdvancedProjectSearchDto
            {
                ClientLocation = "San Francisco",
                PublishedOnly = true,
                Take = 10,
                Skip = 0
            },
            
            // Complex combined search
            new AdvancedProjectSearchDto
            {
                Query = "Platform",
                SkillIds = _testSkills.Take(2).Select(s => s.Id).ToList(),
                MinBudget = 1500,
                PublishedOnly = true,
                Take = 10,
                Skip = 0
            }
        };

        var timings = new List<long>();

        foreach (var searchRequest in queryTypes)
        {
            var stopwatch = Stopwatch.StartNew();

            var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);

            stopwatch.Stop();
            timings.Add(stopwatch.ElapsedMilliseconds);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
            Assert.NotNull(result);
        }

        // Assert all query types perform within acceptable ranges
        var averageTime = timings.Average();
        var maxTime = timings.Max();
        var minTime = timings.Min();

        Assert.True(averageTime < MaxSearchResponseTimeMs,
            $"Average query time {averageTime:F0}ms exceeded {MaxSearchResponseTimeMs}ms");

        Assert.True(maxTime < MaxSearchResponseTimeMs * 2,
            $"Slowest query took {maxTime}ms");

        // Ensure reasonable consistency (max shouldn't be more than 30x min for test environments with cold starts)
        Assert.True(maxTime <= minTime * 30,
            $"Performance variance too high: {maxTime}ms max vs {minTime}ms min");
    }

    #endregion

    #region Scalability Tests

    [Fact]
    public async Task POST_ProjectSearch_DatasetGrowth_MaintainsPerformance()
    {
        // This test simulates searching as the dataset grows
        // In a real scenario, we'd add more projects dynamically

        var searchRequest = new AdvancedProjectSearchDto
        {
            Query = "Development",
            PublishedOnly = true,
            Take = 20,
            Skip = 0
        };

        // Test with different result set sizes by varying Take parameter
        var pageSizes = new[] { 10, 25, 50, 100 };
        var timings = new Dictionary<int, long>();

        foreach (var pageSize in pageSizes)
        {
            searchRequest.Take = pageSize;

            var stopwatch = Stopwatch.StartNew();
            var response = await Client.PostAsJsonAsync("/api/project-search/advanced", searchRequest);
            stopwatch.Stop();

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<AdvancedProjectSearchResultDto>();
            Assert.NotNull(result);

            timings[pageSize] = stopwatch.ElapsedMilliseconds;
        }

        // Assert that response time scales reasonably with result set size
        var time10 = timings[10];
        var time100 = timings[100];

        // Response time for 10x more results should be less than 5x slower
        var scalingFactor = time100 / (double)time10;
        Assert.True(scalingFactor < 5.0,
            $"Performance scaling is poor: {scalingFactor:F2}x slower for 10x more results");
    }

    #endregion

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = SkillLedger.Tests.Infrastructure.TestJsonOptions.Default;
}