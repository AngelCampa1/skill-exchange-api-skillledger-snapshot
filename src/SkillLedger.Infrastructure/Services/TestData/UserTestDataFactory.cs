using Bogus;
using Microsoft.AspNetCore.Identity;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;

namespace SkillLedger.Infrastructure.Services.TestData;

/// <summary>
/// Factory for creating test user personas with realistic data
/// </summary>
public class UserTestDataFactory
{
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly Faker _faker;

    // Hard-coded GUIDs for easy test references
    public static readonly Guid AliceClientId = new Guid("11111111-1111-1111-1111-111111111111");
    public static readonly Guid BobProviderId = new Guid("22222222-2222-2222-2222-222222222222");
    public static readonly Guid CarolAdminId = new Guid("33333333-3333-3333-3333-333333333333");
    public static readonly Guid DavidClientId = new Guid("44444444-4444-4444-4444-444444444444");
    public static readonly Guid EveProviderId = new Guid("55555555-5555-5555-5555-555555555555");

    public UserTestDataFactory(IPasswordHasher<User> passwordHasher)
    {
        _passwordHasher = passwordHasher;
        _faker = new Faker();
    }

    /// <summary>
    /// Creates all 20 test user personas
    /// </summary>
    public List<User> CreateAllUsers()
    {
        var users = new List<User>();

        // Free tier users (5)
        users.AddRange(CreateFreeTierUsers());

        // Professional tier users (5)
        users.AddRange(CreateProfessionalTierUsers());

        // Business tier users (3)
        users.AddRange(CreateBusinessTierUsers());

        // Enterprise tier users (2)
        users.AddRange(CreateEnterpriseTierUsers());

        // Admin and special users (5)
        users.AddRange(CreateAdminAndSpecialUsers());

        return users;
    }

    private List<User> CreateFreeTierUsers()
    {
        var users = new List<User>();

        // User 1: Sarah Chen - New Free User (Active)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000001"),
            "Sarah",
            "Chen",
            "sarah.chen@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: false,
            taxCompliant: false
        ));

        // User 2: Mike Johnson - Free User with Projects (PhoneVerified)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000002"),
            "Mike",
            "Johnson",
            "mike.johnson@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        // User 3: Emily Rodriguez - Free User Near Limit (Active)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000003"),
            "Emily",
            "Rodriguez",
            "emily.rodriguez@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        // User 4: James Park - Suspended Free User (Suspended)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000004"),
            "James",
            "Park",
            "james.park@testmail.com",
            UserStatus.Suspended,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        // User 5: Lisa Wong - Empty State Free User (Active)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000005"),
            "Lisa",
            "Wong",
            "lisa.wong@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        return users;
    }

    private List<User> CreateProfessionalTierUsers()
    {
        var users = new List<User>();

        // User 6: David Kumar - Active Pro Provider (TaxCompliant) - BOB
        users.Add(CreateUser(
            BobProviderId,
            "David",
            "Kumar",
            "david.kumar@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 7: Rachel Goldstein - Active Pro Client (TaxCompliant) - ALICE
        users.Add(CreateUser(
            AliceClientId,
            "Rachel",
            "Goldstein",
            "rachel.goldstein@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 8: Marcus Thompson - Pro in Trial (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000008"),
            "Marcus",
            "Thompson",
            "marcus.thompson@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 9: Sophia Martinez - Pro Past Due (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000009"),
            "Sophia",
            "Martinez",
            "sophia.martinez@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 10: Alex Kim - Pro with Promotion (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000010"),
            "Alex",
            "Kim",
            "alex.kim@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        return users;
    }

    private List<User> CreateBusinessTierUsers()
    {
        var users = new List<User>();

        // User 11: Jennifer Lee - Business Tier Team Lead (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000011"),
            "Jennifer",
            "Lee",
            "jennifer.lee@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 12: Robert Chen - Business Tier API User (TaxCompliant) - DAVID
        users.Add(CreateUser(
            DavidClientId,
            "Robert",
            "Chen",
            "robert.chen@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 13: Maria Santos - Business Tier Cancelled (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000013"),
            "Maria",
            "Santos",
            "maria.santos@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        return users;
    }

    private List<User> CreateEnterpriseTierUsers()
    {
        var users = new List<User>();

        // User 14: Thomas Anderson - Enterprise Admin (TaxCompliant)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000014"),
            "Thomas",
            "Anderson",
            "thomas.anderson@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 15: Patricia Williams - Enterprise Compliance Officer (TaxCompliant) - EVE
        users.Add(CreateUser(
            EveProviderId,
            "Patricia",
            "Williams",
            "patricia.williams@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        return users;
    }

    private List<User> CreateAdminAndSpecialUsers()
    {
        var users = new List<User>();

        // User 16: System Admin - Full Admin Access - CAROL
        users.Add(CreateUser(
            CarolAdminId,
            "Carol",
            "Administrator",
            "admin@skillledger.app",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 17: Content Moderator
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000017"),
            "Moderator",
            "User",
            "moderator@skillledger.app",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: true
        ));

        // User 18: Banned User - John Doe (Banned)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000018"),
            "John",
            "Doe",
            "banned.user@testmail.com",
            UserStatus.Banned,
            emailConfirmed: true,
            phoneNumberConfirmed: false,
            taxCompliant: false
        ));

        // User 19: Edge Case - Zero Balance Provider (Active)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000019"),
            "Zero",
            "Balance",
            "zero.balance@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        // User 20: High-Risk User - Fraud Watch (Active)
        users.Add(CreateUser(
            new Guid("10000000-0000-0000-0000-000000000020"),
            "High",
            "Risk",
            "high.risk@testmail.com",
            UserStatus.Active,
            emailConfirmed: true,
            phoneNumberConfirmed: true,
            taxCompliant: false
        ));

        return users;
    }

    private User CreateUser(
        Guid id,
        string firstName,
        string lastName,
        string email,
        UserStatus status,
        bool emailConfirmed = true,
        bool phoneNumberConfirmed = false,
        bool taxCompliant = false,
        string password = "Test123!")
    {
        var user = new User
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            PhoneNumber = phoneNumberConfirmed ? _faker.Phone.PhoneNumber("###-###-####") : null,
            PhoneNumberConfirmed = phoneNumberConfirmed,
            TaxCompliant = taxCompliant,
            Status = status,
            CreatedAt = DateTime.UtcNow.AddDays(-_faker.Random.Int(1, 365)),
            UpdatedAt = DateTime.UtcNow,
            CreatedFromIP = "TEST_DATA_SEEDER",
            UpdatedFromIP = "TEST_DATA_SEEDER",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            LockoutEnabled = true,
            AccessFailedCount = 0
        };

        // Hash the password
        user.PasswordHash = _passwordHasher.HashPassword(user, password);

        return user;
    }

    /// <summary>
    /// Creates profiles for all users
    /// </summary>
    public List<Profile> CreateProfilesForUsers(List<User> users)
    {
        var profiles = new List<Profile>();

        foreach (var user in users)
        {
            // Skip creating profile for "empty state" user (Lisa Wong)
            if (user.Email == "lisa.wong@testmail.com")
                continue;

            profiles.Add(CreateProfile(user));
        }

        return profiles;
    }

    private Profile CreateProfile(User user)
    {
        var profile = new Profile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Title = GetTitleForUser(user),
            Bio = _faker.Lorem.Paragraph(3),
            Location = _faker.Address.City() + ", " + _faker.Address.StateAbbr(),
            TimeZone = "America/New_York",
            Visibility = ProfileVisibility.Public,
            IsComplete = true,
            ViewCount = _faker.Random.Int(0, 1000),
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };

        return profile;
    }

    private string GetTitleForUser(User user)
    {
        return user.Email switch
        {
            "sarah.chen@testmail.com" => "UX Designer",
            "mike.johnson@testmail.com" => "Frontend Developer",
            "emily.rodriguez@testmail.com" => "Content Writer",
            "james.park@testmail.com" => "Graphic Designer",
            "david.kumar@testmail.com" => "Full-Stack Developer",
            "rachel.goldstein@testmail.com" => "Startup Founder",
            "marcus.thompson@testmail.com" => "Mobile App Developer",
            "sophia.martinez@testmail.com" => "Data Scientist",
            "alex.kim@testmail.com" => "Backend Engineer",
            "jennifer.lee@testmail.com" => "Design Agency Owner",
            "robert.chen@testmail.com" => "CTO",
            "maria.santos@testmail.com" => "Marketing Director",
            "thomas.anderson@testmail.com" => "VP of Engineering",
            "patricia.williams@testmail.com" => "Compliance Manager",
            "admin@skillledger.app" => "System Administrator",
            "moderator@skillledger.app" => "Content Moderator",
            _ => _faker.Name.JobTitle()
        };
    }
}
