# Test-Driven Development Guide for SkillLedger

## Overview

SkillLedger adopts Test-Driven Development (TDD) to ensure high code quality, maintainability, and confidence in our enterprise-grade financial platform. This guide outlines our TDD practices, tools, and workflows.

## Core TDD Principles

### The Red-Green-Refactor Cycle

1. **🔴 Red**: Write a failing test that describes the desired behavior
2. **🟢 Green**: Write the minimal code necessary to make the test pass
3. **🔵 Refactor**: Improve code quality while keeping all tests green
4. **🔄 Repeat**: Continue for each new requirement or bug fix

### TDD Benefits for SkillLedger

- **Security Assurance**: Critical security flows are tested first
- **Financial Accuracy**: Money calculations are validated before implementation
- **Regulatory Compliance**: Audit trails and tax calculations are proven correct
- **Maintainability**: Refactoring is safe with comprehensive test coverage
- **Documentation**: Tests serve as living documentation of business rules

## TDD Workflow for Different Component Types

### Business Logic (Core Domain)
```
1. Write failing unit test for business rule
2. Implement minimal logic to pass
3. Refactor for clarity and performance
4. Add edge cases and validation tests
```

### API Endpoints
```
1. Write failing integration test for endpoint
2. Implement controller action
3. Add authentication/authorization
4. Refactor for clean architecture
```

### Database Operations
```
1. Write failing repository test
2. Implement data access logic
3. Add error handling and validation
4. Optimize queries and refactor
```

### UI Components
```
1. Write failing component test (behavior, not styling)
2. Implement component functionality
3. Add accessibility and error states
4. Refactor for reusability
```

## Testing Pyramid for SkillLedger

### Unit Tests (70%)
- **Business Logic**: Credit calculations, skill matching algorithms
- **Security Functions**: Password validation, token generation
- **Financial Operations**: Tax calculations, currency conversions
- **Validation Rules**: User input validation, business rule enforcement

### Integration Tests (20%)
- **API Endpoints**: Complete request/response cycles
- **Database Operations**: Repository patterns with real database
- **External Service Integration**: Email, SMS, payment processors
- **Authentication Flows**: JWT token validation, role-based access

### End-to-End Tests (10%)
- **Critical User Journeys**: Registration → verification → first project
- **Payment Flows**: Credit purchase → project payment → withdrawal
- **Security Scenarios**: Failed login attempts, session management
- **Compliance Reporting**: Tax document generation, audit exports

## TDD Tools and Setup

### Backend (.NET 9)
- **Test Framework**: xUnit with FluentAssertions
- **Mocking**: Moq for dependencies
- **Test Database**: In-memory SQLite for fast tests
- **Coverage**: Built-in code coverage tools
- See: `docs/backend/TDD_SETUP.md`

### Frontend (Next.js 14)
- **Test Framework**: Jest with React Testing Library
- **Component Testing**: Focus on behavior, not implementation
- **API Mocking**: MSW (Mock Service Worker)
- **E2E Testing**: Playwright for critical flows
- See: `docs/frontend/TDD_SETUP.md`

## TDD Standards and Conventions

### Test Naming
```csharp
// Backend: Should_ExpectedBehavior_When_StateUnderTest
[Fact]
public void Should_ReturnValidationError_When_PasswordTooShort()

// Frontend: describes behavior in plain English
describe('ProjectCreationForm', () => {
  it('should show validation error when project name is empty', () => {
```

### Test Organization
- **Arrange-Act-Assert (AAA)** pattern for unit tests
- **Given-When-Then** for integration and E2E tests
- One assertion per test (logical assertion, not physical)
- Test data builders for complex objects

### Test Categories
- `[Category("Unit")]` - Fast, isolated tests
- `[Category("Integration")]` - Tests with external dependencies
- `[Category("Security")]` - Security-critical test scenarios
- `[Category("Financial")]` - Money and tax calculation tests

## TDD Best Practices

### Do's ✅
- Write tests before implementation code
- Keep tests simple and focused on behavior
- Use descriptive test names that explain the scenario
- Mock only EXTERNAL dependencies (see Mocking Guidelines below)
- Test edge cases and error conditions
- Refactor tests along with production code
- **Verify actual database state changes, not just mock calls**
- **Use real internal services with in-memory database**

### Don'ts ❌
- Don't test implementation details (private methods)
- Don't write tests that duplicate production logic
- Don't ignore failing tests or comment them out
- Don't test third-party library functionality
- Don't write overly complex test setup
- **Don't mock internal business services (see Mocking Guidelines)**
- **Don't rely solely on `.Verify()` to assert test outcomes**

## Mocking Guidelines (CRITICAL)

### The Golden Rule
**Mock EXTERNAL services only. Never mock INTERNAL services.**

A test that only verifies mock calls (`.Verify()`) is **NOT a valid test**. It will pass even when the real service is broken.

### What CAN Be Mocked (External Dependencies)
Services that communicate with systems OUTSIDE our codebase:
- `IEmailService` - Azure Communication Services
- `IFileStorageService` - Azure Blob Storage
- `IVirusScanService` - External virus scanning API
- `ICdnService` - Azure CDN
- `IPaymentService` - Stripe payment processing
- `IGamingDetectionML` - External ML service
- `IGraphDatabaseService` - Neo4j (external database)
- `ILogger<T>` - Logging infrastructure

### What MUST NOT Be Mocked (Internal Dependencies)
Services that contain OUR business logic:
- `IAuditLogService` - Use real service (it writes to DB, verify DB state)
- `ISubscriptionService` - Contains billing logic
- `IReputationCalculationService` - Financial calculations
- `ICreditTransferService` - Financial transactions
- `IProjectEscrowService` - Escrow logic
- `IUserService` - User management
- Any service under `SkillLedger.Infrastructure.Services`

### Anti-Pattern Example (DO NOT DO THIS)
```csharp
// ❌ BAD: This test is WORTHLESS - it tests nothing real
public class BadDocumentServiceTests
{
    private readonly Mock<IAuditLogService> _mockAuditLogService;  // WRONG!
    private readonly Mock<IFileStorageService> _mockFileStorageService;

    [Fact]
    public async Task Upload_ShouldLogAuditEvent()
    {
        // Setup mocks to return expected values
        _mockFileStorageService.Setup(x => x.UploadAsync(...)).ReturnsAsync("url");

        await _service.UploadDocumentAsync(request);

        // ❌ This passes even if the real AuditLogService is broken!
        _mockAuditLogService.Verify(x => x.LogEventAsync(...), Times.Once);
    }
}
```

### Correct Pattern Example (DO THIS)
```csharp
// ✅ GOOD: Real internal services, verify actual database state
public class GoodDocumentServiceTests : LightweightIntegrationTestBase
{
    private readonly AuditLogService _realAuditLogService;        // REAL!
    private readonly MockFileStorageService _mockFileStorage;     // External - OK

    [Fact]
    public async Task Upload_ShouldCreateAuditRecord()
    {
        // Arrange
        var request = CreateValidUploadRequest();

        // Act - Call real service
        await _service.UploadDocumentAsync(request, userId);

        // Assert - Verify REAL database state
        var auditLog = await Context.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "DocumentUpload");

        auditLog.Should().NotBeNull();  // ✅ This tests real behavior!
        auditLog.UserId.Should().Be(userId);
        auditLog.Success.Should().BeTrue();
    }
}
```

### Max Mock Count Rule
- **Maximum 3 mocks per test class** (external services only)
- If you need more mocks, you're probably testing at the wrong level
- Consider using integration tests with real services instead

### Test Validity Checklist
Before committing a test, verify:
- [ ] Does this test use the real database (in-memory is OK)?
- [ ] Does this test use real internal services?
- [ ] Does this test verify database state changes (not just mock calls)?
- [ ] Would this test fail if I introduced a bug in the service?
- [ ] Does this test have 3 or fewer mocked dependencies?

### Security-First TDD
- Authentication tests before implementing login
- Authorization tests before protecting endpoints
- Input validation tests before accepting user data
- Audit logging tests before sensitive operations
- Rate limiting tests before public APIs

## Continuous Integration

### Pre-commit Hooks
- Run fast unit tests locally
- Code formatting and linting
- Security vulnerability scanning

### CI/CD Pipeline
1. **Unit Tests**: Must pass for all commits
2. **Integration Tests**: Must pass for pull requests
3. **Security Tests**: Automated penetration testing
4. **E2E Tests**: Run on staging environment
5. **Performance Tests**: Load testing for critical paths

## Metrics and Quality Gates

### Code Coverage Targets
- **Unit Tests**: 90%+ for business logic and security code
- **Integration Tests**: 80%+ for API endpoints
- **Overall**: 85%+ combined coverage (not a hard requirement)

### Quality Metrics
- **Test Execution Time**: Unit tests < 100ms each
- **Build Time**: Complete test suite < 5 minutes
- **Flaky Test Rate**: < 1% of total tests
- **Bug Escape Rate**: < 2% defects reach production

## Training and Resources

### Internal Resources
- TDD Kata sessions (monthly team practice)
- Code review focus on test quality
- Pair programming for complex TDD scenarios

### External Resources
- "Test Driven Development: By Example" - Kent Beck
- "Growing Object-Oriented Software, Guided by Tests" - Freeman & Pryce
- "The Art of Unit Testing" - Roy Osherove

## Support and Questions

- **Technical Questions**: Ask in #engineering-tdd Slack channel
- **Best Practices**: Schedule pair programming session
- **Tool Issues**: Create ticket in project management tool
- **Training Requests**: Contact tech lead or engineering manager

---

*Last Updated: January 2025*
*Next Review: March 2025*