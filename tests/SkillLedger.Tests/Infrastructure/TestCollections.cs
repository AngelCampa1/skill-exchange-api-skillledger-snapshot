using Xunit;

namespace SkillLedger.Tests.Infrastructure;

/// <summary>
/// Test collection definitions for parallel test execution.
/// Each collection runs in parallel with other collections, but tests within
/// a collection run serially (sharing the same SharedTestHostFixture instance).
///
/// Collections are distributed alphabetically to balance test count:
/// - Api 1: A-F controllers
/// - Api 2: G-Z controllers
/// - Services 1: A-D services
/// - Services 2: E-P services
/// - Services 3: Q-Z services
/// - Security: Security tests
/// - Financial: Financial/Stripe tests
/// - Other: Performance, Hub, misc tests
/// </summary>

[CollectionDefinition("Integration Api 1")]
public class IntegrationApi1Collection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Api 2")]
public class IntegrationApi2Collection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Services 1")]
public class IntegrationServices1Collection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Services 2")]
public class IntegrationServices2Collection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Services 3")]
public class IntegrationServices3Collection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Security")]
public class IntegrationSecurityCollection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Financial")]
public class IntegrationFinancialCollection : ICollectionFixture<SharedTestHostFixture> { }

[CollectionDefinition("Integration Other")]
public class IntegrationOtherCollection : ICollectionFixture<SharedTestHostFixture> { }
