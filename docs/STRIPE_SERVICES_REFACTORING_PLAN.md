# Stripe Services Refactoring Plan for Testability

**Created**: December 24, 2025
**Status**: Proposed
**Priority**: High (Blocking 90% coverage target)

## Executive Summary

**Problem**: StripeWebhookService and StripePromotionService cannot reach 90% code coverage target due to direct instantiation of Stripe SDK clients, making success paths untestable without real Stripe API credentials.

**Current Coverage**:
- StripeWebhookService: 76.31% (target: 90%, gap: 13.69%)
- StripePromotionService: 34.48% (target: 90%, gap: 55.52%)

**Solution**: Refactor services to use dependency injection for Stripe SDK clients, enabling mock injection during testing while maintaining production behavior.

**Impact**:
- Enables testing of all success paths, validation logic, and mapping functions
- Expected coverage after refactoring: 90%+ for both services
- Improves overall architecture (follows SOLID principles)
- No changes to API contracts or calling code

---

## Current Architecture Problems

### Problem 1: Direct Instantiation of Stripe SDK Clients

**StripeWebhookService.cs (lines 28-39)**:
```csharp
public StripeWebhookService(
    IConfiguration configuration,
    ILogger<StripeWebhookService> logger,
    ISubscriptionService subscriptionService,
    IPaymentService paymentService,
    IAuditLogService auditLogService,
    SkillLedgerDbContext context)
{
    // ... other setup ...

    // ❌ PROBLEM: Direct instantiation - can't be mocked
    StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
}
```

Then throughout the service:
```csharp
// Line 992 in ExtractPromotionInfoAsync
var subscriptionService = new Stripe.SubscriptionService();  // ❌ Can't mock
var stripeSubscription = await subscriptionService.GetAsync(...);

// Line 845 in HandlePaymentMethodSetupCompletedAsync
var setupIntentService = new Stripe.SetupIntentService();  // ❌ Can't mock
var setupIntent = await setupIntentService.GetAsync(...);
```

**StripePromotionService.cs (lines 28-34)**:
```csharp
public StripePromotionService(
    ILogger<StripePromotionService> logger,
    IOptions<StripeSettings> stripeSettings)
{
    // ❌ PROBLEM: Direct instantiation
    StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    _couponService = new CouponService();
    _promotionCodeService = new PromotionCodeService();
}
```

### Problem 2: Untestable Code Paths

With fake API keys, all Stripe API calls throw `StripeException`, preventing tests from reaching:

**StripeWebhookService**:
- ✅ Tested: Exception handling, early validation, error logging (76%)
- ❌ Untested:
  - Success path after Stripe API returns data
  - Promotion extraction logic (lines 1000-1049)
  - Payment method syncing logic (lines 819-890)
  - Setup intent handling after API call (lines 845-975)
  - All mapping/transformation logic

**StripePromotionService**:
- ✅ Tested: Parameter validation, exception handling (34%)
- ❌ Untested:
  - All mapping functions (`MapToCouponResult`, `MapToPromoCodeResultAsync`)
  - Validation business logic in `ValidatePromotionCodeAsync` (lines 352-379)
  - Statistics aggregation logic (lines 410-464)
  - Success responses from all CRUD operations

### Problem 3: 0% Branch Coverage

Branch coverage is near-zero because tests never execute conditional logic:
- `if (promoCode.ExpiresAt.HasValue && ...)` - never evaluated
- `if (coupon.Duration == "repeating")` - never evaluated
- All null checks after API calls - never evaluated

---

## Proposed Solution: Stripe SDK Abstraction Layer

### Architecture Overview

```
┌─────────────────────────────────────┐
│   Business Logic Services           │
│  (StripeWebhookService, etc.)       │
└──────────────┬──────────────────────┘
               │ Depends on interfaces
               ▼
┌─────────────────────────────────────┐
│  Stripe Abstraction Interfaces      │
│  - IStripeSubscriptionClient        │
│  - IStripeSetupIntentClient         │
│  - IStripeCouponClient              │
│  - IStripePromotionCodeClient       │
└──────────────┬──────────────────────┘
               │ Implemented by
               ▼
┌─────────────────────────────────────┐
│  Production Implementations         │
│  (Thin wrappers around Stripe SDK)  │
└─────────────────────────────────────┘

               │ Test Implementations
               ▼
┌─────────────────────────────────────┐
│  Mock/Fake Implementations          │
│  (Return controlled test data)      │
└─────────────────────────────────────┘
```

---

## Implementation Plan

### Step 1: Create Stripe Client Interfaces

**File**: `src/SkillLedger.Core/Interfaces/Stripe/IStripeSubscriptionClient.cs`

```csharp
namespace SkillLedger.Core.Interfaces.Stripe;

/// <summary>
/// Abstraction for Stripe SubscriptionService to enable testing.
/// This interface wraps only the Stripe API methods we actually use.
/// </summary>
public interface IStripeSubscriptionClient
{
    /// <summary>
    /// Gets a subscription by ID with optional expand parameters.
    /// </summary>
    Task<Stripe.Subscription> GetAsync(
        string subscriptionId,
        SubscriptionGetOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a subscription.
    /// </summary>
    Task<Stripe.Subscription> UpdateAsync(
        string subscriptionId,
        SubscriptionUpdateOptions options,
        CancellationToken cancellationToken = default);
}
```

**File**: `src/SkillLedger.Core/Interfaces/Stripe/IStripeSetupIntentClient.cs`

```csharp
namespace SkillLedger.Core.Interfaces.Stripe;

public interface IStripeSetupIntentClient
{
    Task<Stripe.SetupIntent> GetAsync(
        string setupIntentId,
        SetupIntentGetOptions? options = null,
        CancellationToken cancellationToken = default);
}
```

**File**: `src/SkillLedger.Core/Interfaces/Stripe/IStripeCouponClient.cs`

```csharp
namespace SkillLedger.Core.Interfaces.Stripe;

public interface IStripeCouponClient
{
    Task<Stripe.Coupon> CreateAsync(
        CouponCreateOptions options,
        CancellationToken cancellationToken = default);

    Task<Stripe.Coupon> GetAsync(
        string couponId,
        CancellationToken cancellationToken = default);

    Task<StripeList<Stripe.Coupon>> ListAsync(
        CouponListOptions options,
        CancellationToken cancellationToken = default);

    Task<Stripe.Coupon> DeleteAsync(
        string couponId,
        CancellationToken cancellationToken = default);
}
```

**File**: `src/SkillLedger.Core/Interfaces/Stripe/IStripePromotionCodeClient.cs`

```csharp
namespace SkillLedger.Core.Interfaces.Stripe;

public interface IStripePromotionCodeClient
{
    Task<Stripe.PromotionCode> CreateAsync(
        PromotionCodeCreateOptions options,
        CancellationToken cancellationToken = default);

    Task<Stripe.PromotionCode> GetAsync(
        string promotionCodeId,
        CancellationToken cancellationToken = default);

    Task<StripeList<Stripe.PromotionCode>> ListAsync(
        PromotionCodeListOptions options,
        CancellationToken cancellationToken = default);

    Task<Stripe.PromotionCode> UpdateAsync(
        string promotionCodeId,
        PromotionCodeUpdateOptions options,
        CancellationToken cancellationToken = default);
}
```

### Step 2: Implement Production Wrappers

**File**: `src/SkillLedger.Infrastructure/Stripe/StripeSubscriptionClient.cs`

```csharp
using SkillLedger.Core.Interfaces.Stripe;
using Stripe;

namespace SkillLedger.Infrastructure.Stripe;

/// <summary>
/// Production implementation - thin wrapper around Stripe.NET SDK.
/// </summary>
public class StripeSubscriptionClient : IStripeSubscriptionClient
{
    private readonly SubscriptionService _subscriptionService;

    public StripeSubscriptionClient()
    {
        // Stripe API key is configured globally via StripeConfiguration.ApiKey
        _subscriptionService = new SubscriptionService();
    }

    public Task<Subscription> GetAsync(
        string subscriptionId,
        SubscriptionGetOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return _subscriptionService.GetAsync(subscriptionId, options, cancellationToken: cancellationToken);
    }

    public Task<Subscription> UpdateAsync(
        string subscriptionId,
        SubscriptionUpdateOptions options,
        CancellationToken cancellationToken = default)
    {
        return _subscriptionService.UpdateAsync(subscriptionId, options, cancellationToken: cancellationToken);
    }
}
```

**Similar implementations for**:
- `StripeSetupIntentClient.cs`
- `StripeCouponClient.cs`
- `StripePromotionCodeClient.cs`

### Step 3: Refactor StripeWebhookService

**Before**:
```csharp
public class StripeWebhookService : IStripeWebhookService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookService> _logger;
    // ... other dependencies

    public StripeWebhookService(
        IConfiguration configuration,
        ILogger<StripeWebhookService> logger,
        // ... other params
    )
    {
        _configuration = configuration;
        _logger = logger;

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;  // ❌
    }

    private async Task<SubscriptionPromotionInfo?> ExtractPromotionInfoAsync(Session session)
    {
        // ❌ Direct instantiation
        var subscriptionService = new Stripe.SubscriptionService();
        var stripeSubscription = await subscriptionService.GetAsync(session.SubscriptionId, ...);
        // ...
    }
}
```

**After**:
```csharp
public class StripeWebhookService : IStripeWebhookService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookService> _logger;
    private readonly IStripeSubscriptionClient _subscriptionClient;  // ✅ Injected
    private readonly IStripeSetupIntentClient _setupIntentClient;    // ✅ Injected
    // ... other dependencies

    public StripeWebhookService(
        IConfiguration configuration,
        ILogger<StripeWebhookService> logger,
        IStripeSubscriptionClient subscriptionClient,      // ✅ Inject
        IStripeSetupIntentClient setupIntentClient,        // ✅ Inject
        // ... other params
    )
    {
        _configuration = configuration;
        _logger = logger;
        _subscriptionClient = subscriptionClient;
        _setupIntentClient = setupIntentClient;

        // API key still configured globally (one place in Startup.cs)
    }

    private async Task<SubscriptionPromotionInfo?> ExtractPromotionInfoAsync(Session session)
    {
        try
        {
            if (string.IsNullOrEmpty(session.SubscriptionId))
            {
                return null;
            }

            // ✅ Use injected client (mockable in tests!)
            var stripeSubscription = await _subscriptionClient.GetAsync(
                session.SubscriptionId,
                new SubscriptionGetOptions
                {
                    Expand = new List<string> { "discounts", "discounts.source.coupon", "discounts.promotion_code" }
                });

            // ... rest of logic unchanged
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract promotion info from session {SessionId}", session.Id);
            return null;
        }
    }
}
```

### Step 4: Refactor StripePromotionService

**Before**:
```csharp
public class StripePromotionService : IStripePromotionService
{
    private readonly CouponService _couponService;                    // ❌
    private readonly PromotionCodeService _promotionCodeService;      // ❌

    public StripePromotionService(
        ILogger<StripePromotionService> logger,
        IOptions<StripeSettings> stripeSettings)
    {
        _logger = logger;
        _stripeSettings = stripeSettings.Value;

        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;       // ❌

        _couponService = new CouponService();                         // ❌
        _promotionCodeService = new PromotionCodeService();           // ❌
    }
}
```

**After**:
```csharp
public class StripePromotionService : IStripePromotionService
{
    private readonly ILogger<StripePromotionService> _logger;
    private readonly IStripeCouponClient _couponClient;              // ✅ Injected
    private readonly IStripePromotionCodeClient _promotionCodeClient; // ✅ Injected

    public StripePromotionService(
        ILogger<StripePromotionService> logger,
        IStripeCouponClient couponClient,                            // ✅ Inject
        IStripePromotionCodeClient promotionCodeClient)              // ✅ Inject
    {
        _logger = logger;
        _couponClient = couponClient;
        _promotionCodeClient = promotionCodeClient;

        // No more direct instantiation!
        // API key configured once in Startup.cs
    }

    public async Task<StripeCouponResult> CreateCouponAsync(CreateCouponRequest request)
    {
        try
        {
            _logger.LogInformation("Creating Stripe coupon: {Name}", request.Name);

            var options = new CouponCreateOptions { /* ... */ };

            // ✅ Use injected client
            var coupon = await _couponClient.CreateAsync(options);

            _logger.LogInformation("Created Stripe coupon {CouponId}: {Name}", coupon.Id, coupon.Name);

            return MapToCouponResult(coupon);  // ✅ Now testable!
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe coupon: {Name}", request.Name);
            throw;
        }
    }
}
```

### Step 5: Register Services in Startup/Program.cs

**File**: `src/SkillLedger.Api/Program.cs` (or Startup.cs)

```csharp
// Configure Stripe API key globally (one place)
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Register Stripe client abstractions
builder.Services.AddSingleton<IStripeSubscriptionClient, StripeSubscriptionClient>();
builder.Services.AddSingleton<IStripeSetupIntentClient, StripeSetupIntentClient>();
builder.Services.AddSingleton<IStripeCouponClient, StripeCouponClient>();
builder.Services.AddSingleton<IStripePromotionCodeClient, StripePromotionCodeClient>();

// Services now receive injected Stripe clients
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IStripePromotionService, StripePromotionService>();
```

### Step 6: Create Test Fakes/Mocks

**File**: `tests/SkillLedger.Tests/Mocks/MockStripeSubscriptionClient.cs`

```csharp
using SkillLedger.Core.Interfaces.Stripe;
using Stripe;

namespace SkillLedger.Tests.Mocks;

/// <summary>
/// Mock Stripe subscription client that returns controlled test data.
/// </summary>
public class MockStripeSubscriptionClient : IStripeSubscriptionClient
{
    private readonly Dictionary<string, Subscription> _subscriptions = new();

    public Func<string, Task<Subscription>>? OnGetAsync { get; set; }

    public void AddSubscription(string id, Subscription subscription)
    {
        _subscriptions[id] = subscription;
    }

    public Task<Subscription> GetAsync(
        string subscriptionId,
        SubscriptionGetOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (OnGetAsync != null)
        {
            return OnGetAsync(subscriptionId);
        }

        if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
        {
            return Task.FromResult(subscription);
        }

        throw new StripeException(
            "resource_missing",
            "No such subscription: " + subscriptionId,
            "subscription_not_found");
    }

    public Task<Subscription> UpdateAsync(
        string subscriptionId,
        SubscriptionUpdateOptions options,
        CancellationToken cancellationToken = default)
    {
        if (_subscriptions.TryGetValue(subscriptionId, out var subscription))
        {
            // Update mock subscription based on options
            // For now, just return it unchanged
            return Task.FromResult(subscription);
        }

        throw new StripeException(
            "resource_missing",
            "No such subscription: " + subscriptionId,
            "subscription_not_found");
    }
}
```

**Similar mocks for**:
- `MockStripeSetupIntentClient.cs`
- `MockStripeCouponClient.cs`
- `MockStripePromotionCodeClient.cs`

### Step 7: Update Tests to Use Mocks

**Before** (StripePromotionServiceIntegrationTests.cs):
```csharp
public StripePromotionServiceIntegrationTests()
{
    _mockLogger = new Mock<ILogger<StripePromotionService>>();
    _stripeSettings = Options.Create(new StripeSettings
    {
        SecretKey = "sk_test_fake_key_for_integration_testing_12345678901234567890",
        WebhookSecret = "whsec_fake_webhook_secret_for_testing"
    });

    // ❌ Service creates its own Stripe clients - can't control responses
    _service = new StripePromotionService(_mockLogger.Object, _stripeSettings);
}

[Fact]
public async Task CreateCouponAsync_WithPercentOff_ShouldCallStripeApi()
{
    var request = new CreateCouponRequest { /* ... */ };

    // ❌ Can only test exception path
    await Assert.ThrowsAsync<StripeException>(() => _service.CreateCouponAsync(request));
}
```

**After**:
```csharp
private readonly MockStripeCouponClient _mockCouponClient;
private readonly MockStripePromotionCodeClient _mockPromoCodeClient;

public StripePromotionServiceIntegrationTests()
{
    _mockLogger = new Mock<ILogger<StripePromotionService>>();

    // ✅ Create mock clients with controlled responses
    _mockCouponClient = new MockStripeCouponClient();
    _mockPromoCodeClient = new MockStripePromotionCodeClient();

    // ✅ Inject mocks into service
    _service = new StripePromotionService(
        _mockLogger.Object,
        _mockCouponClient,
        _mockPromoCodeClient);
}

[Fact]
public async Task CreateCouponAsync_WithPercentOff_ShouldReturnCouponResult()
{
    // Arrange
    var request = new CreateCouponRequest
    {
        Id = "test_percent_coupon",
        Name = "Test Percent Coupon",
        PercentOff = 50m,
        Duration = "once"
    };

    var expectedCoupon = new Stripe.Coupon
    {
        Id = "test_percent_coupon",
        Name = "Test Percent Coupon",
        PercentOff = 50,
        Duration = "once",
        Valid = true,
        Created = DateTime.UtcNow
    };

    // ✅ Configure mock to return success response
    _mockCouponClient.OnCreateAsync = async (options) =>
    {
        options.Id.Should().Be("test_percent_coupon");
        options.PercentOff.Should().Be(50);
        return expectedCoupon;
    };

    // Act
    var result = await _service.CreateCouponAsync(request);

    // Assert - ✅ Can now test success path and mapping logic!
    result.Should().NotBeNull();
    result.Id.Should().Be("test_percent_coupon");
    result.Name.Should().Be("Test Percent Coupon");
    result.PercentOff.Should().Be(50);
    result.IsValid.Should().BeTrue();
}

[Fact]
public async Task ValidatePromotionCodeAsync_ExpiredCode_ShouldReturnFailure()
{
    // Arrange
    var code = "EXPIRED2023";
    var userId = Guid.NewGuid();

    var expiredPromoCode = new Stripe.PromotionCode
    {
        Id = "promo_expired",
        Code = "EXPIRED2023",
        Active = true,
        ExpiresAt = DateTime.UtcNow.AddDays(-30),  // Expired!
        Promotion = new Stripe.Promotion
        {
            Coupon = new Stripe.Coupon
            {
                Id = "coupon_test",
                PercentOff = 20,
                Valid = true
            }
        }
    };

    _mockPromoCodeClient.AddPromotionCode("EXPIRED2023", expiredPromoCode);

    // Act - ✅ Can now test validation business logic!
    var result = await _service.ValidatePromotionCodeAsync(code, userId);

    // Assert
    result.Should().NotBeNull();
    result.IsValid.Should().BeFalse();
    result.ErrorCode.Should().Be("CODE_EXPIRED");
    result.ErrorMessage.Should().Contain("expired");
}
```

---

## Implementation Steps

### Phase 1: Create Abstractions (2-3 hours)
1. Create interface files in `Core/Interfaces/Stripe/`
2. Create production implementation files in `Infrastructure/Stripe/`
3. Update dependency injection in `Program.cs`

### Phase 2: Refactor StripePromotionService (3-4 hours)
1. Update constructor to accept injected clients
2. Replace all `new CouponService()` with `_couponClient`
3. Replace all `new PromotionCodeService()` with `_promotionCodeClient`
4. Run existing tests - should still pass (same behavior)
5. Create mock clients in `tests/SkillLedger.Tests/Mocks/`
6. Update integration tests to use mocks
7. Add 15-20 new tests for success paths, mapping logic, validation
8. Verify 90%+ coverage

### Phase 3: Refactor StripeWebhookService (4-5 hours)
1. Update constructor to accept injected clients
2. Replace all `new SubscriptionService()` with `_subscriptionClient`
3. Replace all `new SetupIntentService()` with `_setupIntentClient`
4. Run existing tests - should still pass
5. Update integration tests to use mocks
6. Add 10-15 new tests for success paths
7. Verify 90%+ coverage

### Phase 4: Verification (1-2 hours)
1. Run full test suite - all tests should pass
2. Run coverage analysis - both services should be 90%+
3. Test in local development environment
4. Update documentation

**Total Estimated Time**: 10-14 hours

---

## Expected Coverage Improvement

### StripePromotionService

**Before**:
- Line coverage: 34.48%
- Branch coverage: 0%
- Untestable: Success paths, mapping logic, validation logic

**After** (with 20-25 new tests):
- Line coverage: **92-95%**
- Branch coverage: **85-90%**
- Newly testable:
  - ✅ All success paths (coupon/promo code creation, retrieval, listing, deactivation)
  - ✅ Mapping functions (`MapToCouponResult`, `MapToPromoCodeResultAsync`)
  - ✅ Validation business logic (expiration, redemption limits, coupon validity)
  - ✅ Statistics aggregation logic
  - ✅ Error handling for missing resources (resource_missing)

### StripeWebhookService

**Before**:
- Line coverage: 76.31%
- Branch coverage: 38.33%
- Untestable: Success paths after Stripe API calls

**After** (with 12-15 new tests):
- Line coverage: **90-93%**
- Branch coverage: **80-85%**
- Newly testable:
  - ✅ Promotion extraction with valid coupons (percent-off, amount-off, repeating, once)
  - ✅ Payment method sync after successful Stripe API response
  - ✅ Setup intent handling with valid setup data
  - ✅ Discount calculation logic (end dates for repeating/once coupons)

---

## Benefits Beyond Coverage

### 1. **Improved Architecture**
- Follows SOLID principles (Dependency Inversion)
- Services depend on abstractions, not concretions
- Easier to swap Stripe for another payment provider in future

### 2. **Better Error Detection**
- Tests can now verify behavior when Stripe returns various responses
- Can test edge cases (empty lists, null values in Stripe responses)
- Can test retry logic and error recovery

### 3. **Faster Test Execution**
- No real API calls = faster tests
- Can run tests offline
- More predictable test behavior

### 4. **Documentation Value**
- Interface definitions serve as contracts
- Mock implementations show expected responses
- Tests demonstrate how to handle various Stripe scenarios

---

## Risks & Mitigation

### Risk 1: Breaking Changes During Refactoring

**Mitigation**:
- Implement behind feature flag initially
- Run old and new code paths in parallel during transition
- Comprehensive integration tests before/after refactoring
- Production testing in staging environment

### Risk 2: Divergence Between Mocks and Real Stripe API

**Mitigation**:
- Keep mock responses aligned with actual Stripe.NET SDK types
- Document any simplifications made in mocks
- Maintain E2E tests with real Stripe test API (separate from unit/integration tests)
- Review Stripe SDK update notes when upgrading

### Risk 3: Increased Complexity

**Mitigation**:
- Keep production implementations thin (simple pass-through)
- Document architecture in this file
- Provide examples in tests
- Code review process to maintain quality

---

## Alternative Approaches Considered

### Option 1: Use Moq/NSubstitute to Mock Stripe SDK Directly
**Rejected**: Stripe SDK classes are not virtual, making them difficult to mock with standard frameworks.

### Option 2: Use Real Stripe Test API
**Rejected**: Requires API keys in CI/CD, slower tests, dependent on external service availability, API rate limits.

### Option 3: Use Stripe.net's Built-in Test Mode
**Rejected**: Still requires API keys and network calls, same issues as Option 2.

### Option 4: Accept Low Coverage
**Rejected**: 34-76% coverage is below project standards and misses critical business logic.

**Selected Approach**: Dependency injection with thin wrapper interfaces provides best balance of testability, maintainability, and performance.

---

## Success Criteria

- [ ] All 4 Stripe client interfaces created
- [ ] All 4 production implementations created
- [ ] All 4 mock implementations created
- [ ] StripePromotionService refactored and tests updated
- [ ] StripeWebhookService refactored and tests updated
- [ ] StripePromotionService coverage ≥ 90%
- [ ] StripeWebhookService coverage ≥ 90%
- [ ] All existing tests still pass
- [ ] 30-40 new tests added for success paths
- [ ] Code review completed
- [ ] Documentation updated
- [ ] Production validation in staging environment

---

## Next Steps

1. **Review this plan** with team and get approval
2. **Create GitHub issue** tracking this refactoring work
3. **Assign developer** to implement Phase 1 (abstractions)
4. **Schedule** Phases 2-4 over next sprint(s)
5. **Update project plan** to reflect coverage will reach 90% after refactoring

---

**Document Owner**: Development Team
**Last Updated**: December 24, 2025
**Next Review**: After Phase 1 completion
