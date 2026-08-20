using SkillLedger.Core.Entities;
using SkillLedger.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace SkillLedger.Tests.TestData;

/// <summary>
/// Test data generator for gaming scenarios and edge cases
/// </summary>
public static class GamingScenarioTestData
{
    /// <summary>
    /// Creates a review farm scenario with multiple fake accounts
    /// </summary>
    public static async Task<GamingScenarioData> CreateReviewFarmScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Review Farm Attack",
            Description = "Multiple fake accounts posting similar reviews"
        };

        // Create master account (farm operator)
        var masterId = Guid.NewGuid();
        scenario.PrimaryUserId = masterId;

        // Create 8 fake accounts
        var fakeUserIds = Enumerable.Range(0, 8).Select(_ => Guid.NewGuid()).ToList();
        scenario.RelatedUserIds.AddRange(fakeUserIds);

        // Shared characteristics (indicators of same operator)
        var sharedDeviceFingerprint = "review_farm_device_fingerprint";
        var baseIpAddress = "192.168.100";

        // Create device fingerprints showing same device/network usage
        // BUG-CRIT-002 FIX: Use indexed foreach to avoid IndexOf(-1) issue
        var userIndex = 0;
        foreach (var userId in fakeUserIds)
        {
            context.DeviceFingerprints.Add(new DeviceFingerprint
            {
                UserId = userId,
                FingerprintHash = sharedDeviceFingerprint,
                IpAddress = $"{baseIpAddress}.{userIndex + 10}",
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                UsedForRegistration = true,
                IsSuspicious = true,
                RiskLevel = 4,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            });
            userIndex++;
        }

        // Create coordinated fake reviews
        var targetProjectIds = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).ToList();
        var fakeReviewTemplates = new[]
        {
            "Excellent work! Highly professional and delivered on time.",
            "Great quality and communication. Would definitely recommend!",
            "Outstanding service and attention to detail. 5 stars!",
            "Perfect work as always. Very reliable and skilled.",
            "Amazing results! Exceeded all expectations completely."
        };

        var coordinatedTime = DateTime.UtcNow.AddDays(-1);

        foreach (var projectId in targetProjectIds)
        {
            for (int i = 0; i < fakeUserIds.Count; i++)
            {
                context.ProjectReviews.Add(new ProjectReview
                {
                    ReviewerId = fakeUserIds[i],
                    ProjectId = projectId,
                    OverallRating = 5,
                    ReviewText = fakeReviewTemplates[i % fakeReviewTemplates.Length],
                    SubmittedAt = coordinatedTime.AddMinutes(i * 15) // 15 minutes apart
                });
            }
        }

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Creates a sock puppet network scenario
    /// </summary>
    public static async Task<GamingScenarioData> CreateSockPuppetNetworkAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Sock Puppet Network",
            Description = "One person controlling multiple fake accounts"
        };

        var masterId = Guid.NewGuid();
        scenario.PrimaryUserId = masterId;

        // Create puppet accounts
        var puppetIds = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid()).ToList();
        scenario.RelatedUserIds.AddRange(puppetIds);

        // All controlled from same device but different VPN endpoints
        var sharedFingerprint = "sock_puppet_master_device";
        var vpnIps = new[]
        {
            "10.1.1.100", "10.2.1.100", "10.3.1.100",
            "10.4.1.100", "10.5.1.100", "10.6.1.100"
        };

        // Master account device
        context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = masterId,
            FingerprintHash = sharedFingerprint,
            IpAddress = "192.168.1.50",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            UsedForRegistration = true,
            RiskLevel = 2
        });

        // Puppet accounts with VPN IPs but same device characteristics
        for (int i = 0; i < puppetIds.Count; i++)
        {
            context.DeviceFingerprints.Add(new DeviceFingerprint
            {
                UserId = puppetIds[i],
                FingerprintHash = sharedFingerprint, // Same device!
                IpAddress = vpnIps[i],
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                UsedForRegistration = true,
                IsSuspicious = true,
                RiskLevel = 5,
                RiskFactors = "[\"VPN_Usage\", \"Shared_Device_Fingerprint\"]",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            });
        }

        // Create network connections between accounts
        for (int i = 0; i < puppetIds.Count; i++)
        {
            context.UserNetworkConnections.Add(new UserNetworkConnection
            {
                User1Id = masterId,
                User2Id = puppetIds[i],
                ConnectionType = "SharedDevice",
                ConnectionStrength = 0.95m,
                DetectedAt = DateTime.UtcNow.AddDays(-15)
            });

            // Cross-connections between puppets
            for (int j = i + 1; j < puppetIds.Count; j++)
            {
                context.UserNetworkConnections.Add(new UserNetworkConnection
                {
                    User1Id = puppetIds[i],
                    User2Id = puppetIds[j],
                    ConnectionType = "SharedDevice",
                    ConnectionStrength = 0.95m,
                    DetectedAt = DateTime.UtcNow.AddDays(-15)
                });
            }
        }

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Creates a velocity attack scenario
    /// </summary>
    public static async Task<GamingScenarioData> CreateVelocityAttackScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Velocity Attack",
            Description = "Rapid-fire review submissions to game the system"
        };

        var attackerId = Guid.NewGuid();
        scenario.PrimaryUserId = attackerId;

        // Create device fingerprint
        context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = attackerId,
            FingerprintHash = "velocity_attacker_device",
            IpAddress = "203.0.113.100",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            RiskLevel = 3
        });

        // Create rapid-fire reviews (30 reviews in 2 hours)
        var baseTime = DateTime.UtcNow.AddHours(-2);
        var reviewContent = new[]
        {
            "Great work! Recommended!",
            "Excellent service! 5 stars!",
            "Perfect! Will hire again!",
            "Outstanding quality work!",
            "Highly professional service!"
        };

        for (int i = 0; i < 30; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = attackerId,
                ProjectId = Guid.NewGuid(),
                OverallRating = 5,
                ReviewText = reviewContent[i % reviewContent.Length],
                SubmittedAt = baseTime.AddMinutes(i * 4) // Every 4 minutes
            });
        }

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Creates a content duplication attack scenario
    /// </summary>
    public static async Task<GamingScenarioData> CreateContentDuplicationScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Content Duplication Attack",
            Description = "Multiple users posting identical or very similar content"
        };

        var userIds = Enumerable.Range(0, 10).Select(_ => Guid.NewGuid()).ToList();
        scenario.RelatedUserIds.AddRange(userIds);

        var identicalContent = "This project demonstrates exceptional quality, professional execution, and timely delivery. The attention to detail and communication throughout the process was outstanding. I would highly recommend this service to anyone looking for reliable and professional work. The final results exceeded my expectations in every way possible.";

        var projectId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.AddDays(-1);

        // All users post identical content (classic content farm)
        for (int i = 0; i < userIds.Count; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = userIds[i],
                ProjectId = projectId,
                OverallRating = 5,
                ReviewText = identicalContent,
                SubmittedAt = baseTime.AddMinutes(i * 20)
            });
        }

        // Add slight variations to test similarity detection
        var variations = new[]
        {
            identicalContent.Replace("exceptional", "remarkable"),
            identicalContent.Replace("outstanding", "excellent"),
            identicalContent.Replace("exceeded my expectations", "surpassed all my expectations")
        };

        for (int i = 0; i < 3; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = Guid.NewGuid(),
                ProjectId = projectId,
                OverallRating = 5,
                ReviewText = variations[i],
                SubmittedAt = baseTime.AddHours(i + 1)
            });
        }

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Creates a review bombing scenario
    /// </summary>
    public static async Task<GamingScenarioData> CreateReviewBombingScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Review Bombing",
            Description = "Coordinated attack to damage reputation with fake negative reviews"
        };

        var attackerIds = Enumerable.Range(0, 12).Select(_ => Guid.NewGuid()).ToList();
        scenario.RelatedUserIds.AddRange(attackerIds);

        var targetProjectId = Guid.NewGuid();
        var coordinatedTime = DateTime.UtcNow.AddHours(-6);

        var negativeComments = new[]
        {
            "Terrible work! Complete waste of money! Avoid at all costs!",
            "Unprofessional and delivered late. Very disappointed with quality.",
            "Poor communication and subpar results. Would not recommend.",
            "Overpriced for the quality delivered. Much better options available.",
            "Failed to meet basic requirements. Had to hire someone else to fix it."
        };

        // Coordinated negative review attack within 3 hours
        for (int i = 0; i < attackerIds.Count; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = attackerIds[i],
                ProjectId = targetProjectId,
                OverallRating = 1, // All 1-star ratings
                ReviewText = negativeComments[i % negativeComments.Length],
                SubmittedAt = coordinatedTime.AddMinutes(i * 15)
            });
        }

        // Add some device fingerprints showing possible coordination
        var sharedIpBase = "198.51.100";
        for (int i = 0; i < 4; i++)
        {
            var sharedFingerprint = $"review_bombing_device_{i}";
            for (int j = 0; j < 3; j++)
            {
                var userIndex = i * 3 + j;
                if (userIndex < attackerIds.Count)
                {
                    context.DeviceFingerprints.Add(new DeviceFingerprint
                    {
                        UserId = attackerIds[userIndex],
                        FingerprintHash = sharedFingerprint,
                        IpAddress = $"{sharedIpBase}.{10 + i}",
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                        RiskLevel = 4,
                        IsSuspicious = true
                    });
                }
            }
        }

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Creates edge cases and unusual patterns
    /// </summary>
    public static async Task<GamingScenarioData> CreateEdgeCaseScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Edge Cases and Unusual Patterns",
            Description = "Testing various edge cases and unusual but legitimate patterns"
        };

        // Case 1: Legitimate power user (high volume but authentic)
        var powerUserId = Guid.NewGuid();
        scenario.PrimaryUserId = powerUserId;
        scenario.RelatedUserIds.Add(powerUserId);

        context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = powerUserId,
            FingerprintHash = "legitimate_power_user_device",
            IpAddress = "203.0.113.200",
            UserAgent = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36",
            RiskLevel = 1
        });

        // Power user posts many reviews but with variety and reasonable timing
        var powerUserTemplates = new[]
        {
            "Delivered exactly what was promised. Good communication throughout the project lifecycle.",
            "Professional approach and timely delivery. Some minor revisions were needed but handled well.",
            "Solid work with room for improvement in certain areas. Overall satisfied with the outcome.",
            "Exceeded expectations in creativity. Could improve on time management for future projects.",
            "Technical skills are strong. Documentation could be more comprehensive next time."
        };

        var baseTime = DateTime.UtcNow.AddDays(-30);
        for (int i = 0; i < 25; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = powerUserId,
                ProjectId = Guid.NewGuid(),
                OverallRating = 3 + (i % 3), // Varied ratings 3-5
                ReviewText = powerUserTemplates[i % powerUserTemplates.Length] + $" Project completion date was optimal for timeline {i + 1}.",
                SubmittedAt = baseTime.AddDays(i * 1.2) // Spread over 30 days
            });
        }

        // Case 2: Family/Team sharing device (legitimate shared device)
        var familyUserIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        scenario.RelatedUserIds.AddRange(familyUserIds);

        var familyFingerprint = "family_shared_computer";
        foreach (var userId in familyUserIds)
        {
            context.DeviceFingerprints.Add(new DeviceFingerprint
            {
                UserId = userId,
                FingerprintHash = familyFingerprint,
                IpAddress = "192.168.1.100", // Same home IP
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
                RiskLevel = 2 // Slightly elevated but not suspicious
            });
        }

        // Family members have different review patterns and timing
        var familyReviewTimes = new[]
        {
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(-15),
            DateTime.UtcNow.AddDays(-5)
        };

        for (int i = 0; i < familyUserIds.Length; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = familyUserIds[i],
                ProjectId = Guid.NewGuid(),
                OverallRating = 4,
                ReviewText = $"Family member {i + 1} review with unique perspective and personal experience details.",
                SubmittedAt = familyReviewTimes[i]
            });
        }

        // Case 3: International user with VPN (legitimate but flagged)
        var vpnUserId = Guid.NewGuid();
        scenario.RelatedUserIds.Add(vpnUserId);

        context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = vpnUserId,
            FingerprintHash = "legitimate_vpn_user",
            IpAddress = "10.8.8.8", // VPN IP
            UserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36",
            RiskLevel = 3, // Higher due to VPN but legitimate
            CountryCode = "DE"
        });

        context.ProjectReviews.Add(new ProjectReview
        {
            ReviewerId = vpnUserId,
            ProjectId = Guid.NewGuid(),
            OverallRating = 4,
            ReviewText = "Working from Germany, used VPN for security. Great experience with international collaboration.",
            SubmittedAt = DateTime.UtcNow.AddDays(-7)
        });

        await context.SaveChangesAsync();
        return scenario;
    }

    /// <summary>
    /// Create a scenario with mixed legitimate and gaming patterns
    /// </summary>
    public static async Task<GamingScenarioData> CreateMixedPatternScenarioAsync(SkillLedgerDbContext context)
    {
        var scenario = new GamingScenarioData
        {
            Name = "Mixed Pattern Scenario",
            Description = "User starts legitimate then transitions to gaming behavior"
        };

        var userId = Guid.NewGuid();
        scenario.PrimaryUserId = userId;

        // Device fingerprint
        context.DeviceFingerprints.Add(new DeviceFingerprint
        {
            UserId = userId,
            FingerprintHash = "mixed_behavior_device",
            IpAddress = "203.0.113.50",
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36",
            RiskLevel = 2
        });

        // Phase 1: Legitimate behavior (3 months ago)
        var legitimatePhase = DateTime.UtcNow.AddDays(-90);
        var legitimateComments = new[]
        {
            "Professional work delivered on schedule. Good attention to project requirements and client communication.",
            "Quality output with minor revisions needed. Overall positive experience and would consider future collaboration.",
            "Solid technical execution. Documentation was comprehensive and delivery timeline was met as promised."
        };

        for (int i = 0; i < 3; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = userId,
                ProjectId = Guid.NewGuid(),
                OverallRating = 4,
                ReviewText = legitimateComments[i],
                SubmittedAt = legitimatePhase.AddDays(i * 10)
            });
        }

        // Phase 2: Gradual transition (1 month ago)
        var transitionPhase = DateTime.UtcNow.AddDays(-30);
        for (int i = 0; i < 6; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = userId,
                ProjectId = Guid.NewGuid(),
                OverallRating = 5,
                ReviewText = "Great work! Highly recommended!", // Becoming repetitive
                SubmittedAt = transitionPhase.AddDays(i * 3)
            });
        }

        // Phase 3: Gaming behavior (last week)
        var gamingPhase = DateTime.UtcNow.AddDays(-7);
        for (int i = 0; i < 10; i++)
        {
            context.ProjectReviews.Add(new ProjectReview
            {
                ReviewerId = userId,
                ProjectId = Guid.NewGuid(),
                OverallRating = 5,
                ReviewText = "Excellent work! Highly professional!", // Very similar content
                SubmittedAt = gamingPhase.AddMinutes(i * 30) // High frequency
            });
        }

        await context.SaveChangesAsync();
        return scenario;
    }
}

/// <summary>
/// Data structure for gaming test scenarios
/// </summary>
public class GamingScenarioData
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid PrimaryUserId { get; set; }
    public List<Guid> RelatedUserIds { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Helper class for creating test data patterns
/// </summary>
public static class TestDataPatterns
{
    public static readonly string[] HighRiskUserAgents = new[]
    {
        "HeadlessChrome/90.0.4430.212",
        "PhantomJS/2.1.1",
        "SlimerJS/0.10.3",
        "CasperJS/1.1.4",
        "Selenium/3.141.59 (python/3.8)"
    };

    public static readonly string[] VpnIpRanges = new[]
    {
        "10.0.0.",
        "172.16.0.",
        "192.168.100.",
        "198.51.100.",
        "203.0.113."
    };

    public static readonly string[] FakeReviewTemplates = new[]
    {
        "Excellent work! Highly recommended!",
        "Great quality and fast delivery!",
        "Professional service, will hire again!",
        "Outstanding results, 5 stars!",
        "Perfect work as always!"
    };

    public static readonly string[] LegitimateReviewTemplates = new[]
    {
        "Good work overall. Delivered on time with clear communication throughout the project.",
        "Professional approach and solid execution. Minor revisions were handled promptly.",
        "Quality output that met project requirements. Would consider working together again.",
        "Reliable delivery and good attention to detail. Communication could be improved.",
        "Solid technical skills demonstrated. Documentation was comprehensive and helpful."
    };
}