using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Infrastructure.Configuration;
using SkillLedger.Infrastructure.Data;
using SkillLedger.Infrastructure.Services;
using SkillLedger.Tests.Mocks;
using Xunit;

namespace SkillLedger.Tests.Integration.Services;

/// <summary>
/// Integration tests for StripeCheckoutService - Checkout Session Creation.
///
/// CORRECT PATTERN (per TDD_GUIDE.md):
/// - Uses real database (in-memory EF Core)
/// - Uses real internal services (subscription, audit log)
/// - Mocks only EXTERNAL services (Stripe SDK)
/// - Verifies actual database state, not mock interactions
///
/// Max mocked external dependencies: 1 (Stripe SDK - external payment provider)
/// </summary>
public class StripeCheckoutServiceIntegrationTests : IDisposable
{
    private readonly SkillLedgerDbContext _context;
    private readonly StripeCheckoutService _service;

    // REAL internal services
    private readonly SubscriptionService _subscriptionService;
    private readonly MockAuditLogService _auditLogService;
    private readonly MockCreditWalletService _walletService;
    private readonly MockPaymentService _paymentService;

    // Test data
    private readonly User _testUser;
    private readonly SubscriptionTier _professionalTier;
    private readonly SubscriptionTier _businessTier;

    public StripeCheckoutServiceIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<SkillLedgerDbContext>()
            .UseInMemoryDatabase(databaseName: $"StripeCheckoutTests_{Guid.NewGuid()}")
            .Options;

        _context = new SkillLedgerDbContext(options);
        _context.Database.EnsureCreated();

        // Setup REAL internal services
        _auditLogService = new MockAuditLogService(_context);
        _walletService = new MockCreditWalletService(_context);
        _paymentService = new MockPaymentService();

        var subLogger = new LoggerFactory().CreateLogger<SubscriptionService>();
        _subscriptionService = new SubscriptionService(
            _context,
            _paymentService,
            _walletService,
            _auditLogService,
            subLogger);

        // Configure Stripe settings (disabled for tests)
        var stripeSettings = Options.Create(new StripeSettings
        {
            SecretKey = "sk_test_fake_key_for_testing_only_1234567890abcdefghijklmnop",
            PublishableKey = "pk_test_fake_key",
            IsEnabled = false, // Disable actual Stripe calls
            IsTestMode = true
        });

        var logger = new LoggerFactory().CreateLogger<StripeCheckoutService>();

        _service = new StripeCheckoutService(
            logger,
            stripeSettings,
            _context,
            _subscriptionService,
            _auditLogService);

        // Initialize test data
        _testUser = new User
        {
            Id = Guid.NewGuid(),
            UserName = "checkoutuser",
            Email = "checkout@example.com",
            FirstName = "Checkout",
            LastName = "User",
            EmailConfirmed = true,
            ExternalCustomerId = null // Will be created during checkout
        };

        _professionalTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Professional",
            Type = SubscriptionTierType.Professional,
            Price = 19.99m,
            AnnualPrice = 199.99m,
            Features = "[\"basic-feature\",\"advanced-feature\"]",
            MaxActiveProjects = 5,
            MaxTeamMembers = 3,
            IsActive = true,
            SortOrder = 1
        };

        _businessTier = new SubscriptionTier
        {
            Id = Guid.NewGuid(),
            Name = "Business",
            Type = SubscriptionTierType.Business,
            Price = 49.99m,
            AnnualPrice = 499.99m,
            Features = "[\"basic-feature\",\"advanced-feature\",\"premium-feature\"]",
            MaxActiveProjects = 20,
            MaxTeamMembers = 10,
            IsActive = true,
            SortOrder = 2
        };

        _context.Users.Add(_testUser);
        _context.SubscriptionTiers.AddRange(_professionalTier, _businessTier);

        // Create wallet for user
        _walletService.CreateWalletAsync(_testUser.Id).Wait();

        _context.SaveChanges();
    }

    #region CreateSubscriptionCheckoutAsync Tests

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_MonthlyBilling_ShouldCreateSession()
    {
        // Arrange
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl,
            "192.168.1.1");

        // Assert - Verify result
        result.Should().NotBeNull();
        result.SessionId.Should().NotBeNullOrEmpty();
        result.SessionUrl.Should().NotBeNullOrEmpty();
        result.Success.Should().BeTrue();

        // Verify Stripe customer ID was created and saved
        var user = await _context.Users.FindAsync(_testUser.Id);
        user.Should().NotBeNull();
        user!.ExternalCustomerId.Should().NotBeNullOrEmpty();

        // Verify audit log created
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id &&
                                     a.Action.Contains("CHECKOUT_SESSION_CREATED"));
        auditLog.Should().NotBeNull();
        auditLog!.Success.Should().BeTrue();
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_AnnualBilling_ShouldUseAnnualPrice()
    {
        // Arrange
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Annual,
            successUrl,
            cancelUrl,
            "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Amount.Should().Be(_professionalTier.AnnualPrice!.Value);
        result.BillingCycle.Should().Be(BillingCycle.Annual);
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_InvalidTierId_ShouldReturnFailure()
    {
        // Arrange
        var invalidTierId = Guid.NewGuid();
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            invalidTierId,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Invalid subscription tier");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_InactiveTier_ShouldReturnFailure()
    {
        // Arrange - Deactivate tier
        _professionalTier.IsActive = false;
        await _context.SaveChangesAsync();

        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Subscription tier is not active");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_ExistingCustomer_ShouldReuseCustomerId()
    {
        // Arrange - User already has Stripe customer ID
        _testUser.ExternalCustomerId = "cus_existing_customer_id";
        await _context.SaveChangesAsync();

        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();

        // Verify customer ID not changed
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.ExternalCustomerId.Should().Be("cus_existing_customer_id");
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_ResultPopulated_ShouldContainAllFields()
    {
        // Arrange
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl,
            "192.168.1.1");

        // Assert - Verify result contains all required fields
        result.TierId.Should().Be(_professionalTier.Id);
        result.TierName.Should().Be(_professionalTier.Name);
        result.Amount.Should().Be(_professionalTier.Price);
        result.BillingCycle.Should().Be(BillingCycle.Monthly);
        result.Currency.Should().Be("usd");
    }

    #endregion

    #region CreatePaymentMethodSetupSessionAsync Tests

    [Fact]
    public async Task CreatePaymentMethodSetupSessionAsync_NewUser_ShouldCreateSession()
    {
        // Arrange
        var successUrl = "https://example.com/payment-success";
        var cancelUrl = "https://example.com/payment-cancel";

        // Act
        var result = await _service.CreatePaymentMethodSetupSessionAsync(
            _testUser.Id,
            successUrl,
            cancelUrl,
            "192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result.SessionId.Should().NotBeNullOrEmpty();
        result.SessionUrl.Should().NotBeNullOrEmpty();
        result.Success.Should().BeTrue();

        // Verify Stripe customer created
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.ExternalCustomerId.Should().NotBeNullOrEmpty();

        // Verify audit log
        var auditLog = await _context.AuditLogs
            .FirstOrDefaultAsync(a => a.UserId == _testUser.Id &&
                                     a.Action.Contains("PAYMENT_METHOD_SETUP_SESSION"));
        auditLog.Should().NotBeNull();
    }

    [Fact]
    public async Task CreatePaymentMethodSetupSessionAsync_ExistingCustomer_ShouldReuseCustomerId()
    {
        // Arrange - User already has customer ID
        _testUser.ExternalCustomerId = "cus_existing_payment_setup";
        await _context.SaveChangesAsync();

        var successUrl = "https://example.com/payment-success";
        var cancelUrl = "https://example.com/payment-cancel";

        // Act
        var result = await _service.CreatePaymentMethodSetupSessionAsync(
            _testUser.Id,
            successUrl,
            cancelUrl);

        // Assert
        result.Success.Should().BeTrue();

        // Customer ID should not change
        var user = await _context.Users.FindAsync(_testUser.Id);
        user!.ExternalCustomerId.Should().Be("cus_existing_payment_setup");
    }

    [Fact]
    public async Task CreatePaymentMethodSetupSessionAsync_ResultPopulated_ShouldContainCorrectFlags()
    {
        // Arrange
        var successUrl = "https://example.com/payment-success";
        var cancelUrl = "https://example.com/payment-cancel";

        // Act
        var result = await _service.CreatePaymentMethodSetupSessionAsync(
            _testUser.Id,
            successUrl,
            cancelUrl);

        // Assert
        result.IsPaymentMethodSetup.Should().BeTrue("this is a payment method setup session");
        result.CustomerId.Should().NotBeNullOrEmpty("customer ID should be populated");
    }

    #endregion

    #region GetCheckoutSessionAsync Tests

    [Fact]
    public async Task GetCheckoutSessionAsync_ValidSessionId_ShouldReturnDetails()
    {
        // Arrange - First create a checkout session
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        var createResult = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl);

        // Act - Get the session details
        var sessionDetails = await _service.GetCheckoutSessionAsync(createResult.SessionId);

        // Assert
        sessionDetails.Should().NotBeNull();
        sessionDetails!.SessionId.Should().Be(createResult.SessionId);
        sessionDetails.Status.Should().NotBeNullOrEmpty();
        sessionDetails.CustomerId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCheckoutSessionAsync_InvalidSessionId_ShouldReturnNull()
    {
        // Arrange
        var invalidSessionId = "cs_test_invalid_session_12345";

        // Act
        var sessionDetails = await _service.GetCheckoutSessionAsync(invalidSessionId);

        // Assert
        sessionDetails.Should().BeNull();
    }

    [Fact]
    public async Task GetCheckoutSessionAsync_EmptySessionId_ShouldReturnNull()
    {
        // Arrange
        var emptySessionId = string.Empty;

        // Act
        var sessionDetails = await _service.GetCheckoutSessionAsync(emptySessionId);

        // Assert
        sessionDetails.Should().BeNull();
    }

    #endregion

    #region Trial Period Tests

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_WithTrialPeriod_ShouldCreateSession()
    {
        // Arrange
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl,
            "192.168.1.1",
            trialPeriodDays: 30);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.SessionId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateSubscriptionCheckoutAsync_WithoutTrialPeriod_ShouldCreateSession()
    {
        // Arrange
        var successUrl = "https://example.com/success";
        var cancelUrl = "https://example.com/cancel";

        // Act
        var result = await _service.CreateSubscriptionCheckoutAsync(
            _testUser.Id,
            _professionalTier.Id,
            BillingCycle.Monthly,
            successUrl,
            cancelUrl,
            trialPeriodDays: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
