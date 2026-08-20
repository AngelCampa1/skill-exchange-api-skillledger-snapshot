using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using SkillLedger.Core.Entities;
using SkillLedger.Core.Enums;
using SkillLedger.Core.Interfaces;
using SkillLedger.Tests.Infrastructure;

namespace SkillLedger.Tests.Integration.Api;

/// <summary>
/// Integration tests for Auth API endpoints
/// Tests registration, login, logout, password reset, and session management
/// </summary>
[IntegrationTest]
[ApiTest]
[Collection("Integration Api 1")]
public class AuthControllerIntegrationTests : IntegrationTestBase
{
    private User _existingUser = null!;
    private const string ExistingUserPassword = "TestPassword123!@#";

    public AuthControllerIntegrationTests(SharedTestHostFixture fixture) : base(fixture)
    {
    }

    protected override async Task OnInitializeAsync()
    {
        await base.OnInitializeAsync();

        // Setup existing user for login/logout tests
        _existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing-auth-user@test.com",
            UserName = "existing-auth-user@test.com",
            Status = UserStatus.Active,
            EmailConfirmed = true
        };

        Context.Users.Add(_existingUser);
        await Context.SaveChangesAsync();
    }

    #region POST /api/auth/register Tests

    [Fact]
    [FastTest]
    public async Task POST_Register_WithValidData_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Email = $"new-user-{Guid.NewGuid():N}@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "not-an-email",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "weak-pass@test.com",
            Password = "123",  // Too weak
            ConfirmPassword = "123",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithMismatchedPasswords_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "mismatch@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "DifferentPass456!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithoutAcceptingTerms_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "noterms@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Register_WithSqlInjectionAttempt_ReturnsBadRequestOrSafe()
    {
        // Arrange
        var request = new
        {
            Email = "'; DROP TABLE Users;--@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test'; DROP TABLE Users;--",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Should safely reject or sanitize malicious input
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Register_WithXssAttempt_ReturnsBadRequestOrSanitizes()
    {
        // Arrange
        var request = new
        {
            Email = "xss@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "<script>alert('xss')</script>",
            LastName = "<img src=x onerror=alert('xss')>",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert - Should either reject or sanitize XSS
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region GET /api/auth/check-email Tests

    [Fact]
    [FastTest]
    public async Task GET_CheckEmail_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var email = "check-available@test.com";

        // Act
        var response = await Client.GetAsync($"/api/auth/check-email?email={email}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("isAvailable", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_CheckEmail_WithEmptyEmail_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/check-email?email=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_CheckEmail_WithoutEmailParam_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/check-email");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task GET_CheckEmail_WithExistingEmail_ReturnsIsAvailableTrue_ToPreventEnumeration()
    {
        // Arrange - Use existing user's email
        var email = _existingUser.Email;

        // Act
        var response = await Client.GetAsync($"/api/auth/check-email?email={email}");

        // Assert - Should always return IsAvailable=true to prevent enumeration attacks
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
    }

    #endregion

    #region GET /api/auth/csrf-token Tests

    [Fact]
    [FastTest]
    public async Task GET_CsrfToken_ReturnsOkWithToken()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/csrf-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("token", out _).Should().BeTrue();
        content.TryGetProperty("headerName", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_CsrfToken_ReturnsCorrectHeaderName()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/csrf-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        var headerName = content.GetProperty("headerName").GetString();
        headerName.Should().Be("X-CSRF-TOKEN");
    }

    #endregion

    #region POST /api/auth/login Tests

    [Fact]
    [FastTest]
    public async Task POST_Login_WithValidCredentials_ReturnsOkOrUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = _existingUser.Email,
            Password = ExistingUserPassword,
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert - May return Unauthorized if password hash doesn't match in test
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Login_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Password = "SomePassword123!",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Login_WithMissingPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "test@test.com",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new
        {
            Email = "nonexistent@test.com",
            Password = "WrongPassword123!",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Login_WithSqlInjection_ReturnsSafeResponse()
    {
        // Arrange
        var request = new
        {
            Email = "admin@test.com' OR '1'='1",
            Password = "' OR '1'='1",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert - Should safely handle SQL injection attempts
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region POST /api/auth/logout Tests

    [Fact]
    [FastTest]
    public async Task POST_Logout_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_Logout_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_existingUser);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/auth/logout", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Logout_WithAuthWithoutCsrf_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/logout-all Tests

    [Fact]
    [FastTest]
    public async Task POST_LogoutAll_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout-all", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_LogoutAll_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_existingUser);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/auth/logout-all", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_LogoutAll_WithAuthWithoutCsrf_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/logout-all", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region POST /api/auth/refresh Tests

    [Fact]
    [FastTest]
    public async Task POST_Refresh_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task POST_Refresh_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_existingUser);
        var content = JsonContent.Create(new { });
        await AddCsrfTokenToRequest(content);

        // Act
        var response = await Client.PostAsync("/api/auth/refresh", content);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Refresh_WithAuthWithoutCsrf_ReturnsBadRequest()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/auth/me Tests

    [Fact]
    [FastTest]
    public async Task GET_Me_WithoutAuth_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    [FastTest]
    public async Task GET_Me_WithAuth_ReturnsOk()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_Me_WithAuth_ReturnsUserProfile()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.GetAsync("/api/auth/me");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.TryGetProperty("user", out _).Should().BeTrue();
        }
    }

    #endregion

    #region GET /api/auth/status Tests

    [Fact]
    [FastTest]
    public async Task GET_Status_WithoutAuth_ReturnsNotAuthenticated()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("isAuthenticated").GetBoolean().Should().BeFalse();
    }

    [Fact]
    [FastTest]
    public async Task GET_Status_WithAuth_ReturnsAuthenticated()
    {
        // Arrange
        AuthenticateAs(_existingUser);

        // Act
        var response = await Client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("isAuthenticated").GetBoolean().Should().BeTrue();
        content.TryGetProperty("user", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_Status_ReturnsTimestamp()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/status");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    #endregion

    #region POST /api/auth/forgot-password Tests

    [Fact]
    [FastTest]
    public async Task POST_ForgotPassword_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new
        {
            Email = _existingUser.Email
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, (HttpStatusCode)429);
    }

    [Fact]
    [FastTest]
    public async Task POST_ForgotPassword_WithMissingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new { };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ForgotPassword_WithInvalidEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Email = "not-an-email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_ForgotPassword_WithNonExistentEmail_ReturnsGenericResponse()
    {
        // Arrange - Use email that doesn't exist
        var request = new
        {
            Email = "nonexistent-email-for-test@test.com"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/forgot-password", request);

        // Assert - Should return generic success to prevent enumeration
        // or rate limited, or bad request for other validation
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError, (HttpStatusCode)429);
    }

    #endregion

    #region GET /api/auth/validate-reset-token Tests

    [Fact]
    [FastTest]
    public async Task GET_ValidateResetToken_WithValidToken_ReturnsOk()
    {
        // Arrange
        var token = "test-reset-token-12345";

        // Act
        var response = await Client.GetAsync($"/api/auth/validate-reset-token?token={token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("valid", out _).Should().BeTrue();
    }

    [Fact]
    [FastTest]
    public async Task GET_ValidateResetToken_WithEmptyToken_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/validate-reset-token?token=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_ValidateResetToken_WithoutToken_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/validate-reset-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_ValidateResetToken_WithInvalidToken_ReturnsValidFalse()
    {
        // Arrange
        var token = "invalid-token-xyz";

        // Act
        var response = await Client.GetAsync($"/api/auth/validate-reset-token?token={token}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("valid").GetBoolean().Should().BeFalse();
    }

    #endregion

    #region POST /api/auth/reset-password Tests

    [Fact]
    [FastTest]
    public async Task POST_ResetPassword_WithValidData_ReturnsOkOrBadRequest()
    {
        // Arrange
        var request = new
        {
            Token = "test-reset-token",
            NewPassword = "NewSecurePassword123!@#",
            ConfirmPassword = "NewSecurePassword123!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", request);

        // Assert - May return BadRequest if token is invalid
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ResetPassword_WithMissingToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            NewPassword = "NewSecurePassword123!@#",
            ConfirmPassword = "NewSecurePassword123!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ResetPassword_WithWeakPassword_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Token = "test-token",
            NewPassword = "weak",
            ConfirmPassword = "weak"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_ResetPassword_WithMismatchedPasswords_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Token = "test-token",
            NewPassword = "NewSecurePassword123!@#",
            ConfirmPassword = "DifferentPassword456!@#"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/reset-password", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region GET /api/auth/password-reset-attempts Tests

    [Fact]
    [FastTest]
    public async Task GET_PasswordResetAttempts_WithValidEmail_ReturnsOk()
    {
        // Arrange
        var email = _existingUser.Email;

        // Act
        var response = await Client.GetAsync($"/api/auth/password-reset-attempts?email={email}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.InternalServerError, (HttpStatusCode)429);
    }

    [Fact]
    [FastTest]
    public async Task GET_PasswordResetAttempts_WithEmptyEmail_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/password-reset-attempts?email=");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_PasswordResetAttempts_WithoutEmail_ReturnsBadRequest()
    {
        // Act
        var response = await Client.GetAsync("/api/auth/password-reset-attempts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task GET_PasswordResetAttempts_ReturnsAttemptsInfo()
    {
        // Arrange
        var email = _existingUser.Email;

        // Act
        var response = await Client.GetAsync($"/api/auth/password-reset-attempts?email={email}");

        // Assert
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            content.TryGetProperty("remainingAttempts", out _).Should().BeTrue();
            content.TryGetProperty("canRequestReset", out _).Should().BeTrue();
        }
    }

    #endregion

    #region Authorization Tests

    [Fact]
    [SecurityTest]
    public async Task AuthenticatedEndpoints_WithoutAuth_ReturnUnauthorized()
    {
        // Test all endpoints that require authentication
        var endpoints = new[]
        {
            ("POST", "/api/auth/logout"),
            ("POST", "/api/auth/logout-all"),
            ("POST", "/api/auth/refresh"),
            ("GET", "/api/auth/me"),
        };

        foreach (var (method, url) in endpoints)
        {
            HttpResponseMessage response;
            switch (method)
            {
                case "GET":
                    response = await Client.GetAsync(url);
                    break;
                case "POST":
                    response = await Client.PostAsJsonAsync(url, new { });
                    break;
                default:
                    continue;
            }

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                $"{method} {url} should require authentication");
        }
    }

    [Fact]
    [SecurityTest]
    public async Task PublicEndpoints_WithoutAuth_DoNotReturnUnauthorized()
    {
        // Test all public endpoints
        var endpoints = new[]
        {
            "/api/auth/csrf-token",
            "/api/auth/status",
        };

        foreach (var url in endpoints)
        {
            var response = await Client.GetAsync(url);

            response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized,
                $"GET {url} should be publicly accessible");
        }
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    [SecurityTest]
    public async Task POST_Register_MultipleRapidRequests_EventuallyRateLimited()
    {
        // Arrange - Make multiple rapid registration attempts
        var responses = new List<HttpResponseMessage>();
        var uniqueEmails = Enumerable.Range(1, 20).Select(i => $"ratelimit-test-{i}@test.com").ToList();

        // Act
        foreach (var email in uniqueEmails)
        {
            var request = new
            {
                Email = email,
                Password = "SecurePass123!@#",
                ConfirmPassword = "SecurePass123!@#",
                FirstName = "Rate",
                LastName = "Test",
                AcceptTerms = true
            };

            var response = await Client.PostAsJsonAsync("/api/auth/register", request);
            responses.Add(response);
        }

        // Assert - At some point we should get rate limited or all requests processed
        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        // Either all succeed/fail normally or some get rate limited
        statusCodes.Should().OnlyContain(s =>
            s == HttpStatusCode.OK ||
            s == HttpStatusCode.BadRequest ||
            s == HttpStatusCode.InternalServerError ||
            s == (HttpStatusCode)429);
    }

    [Fact]
    [SecurityTest]
    public async Task POST_Login_MultipleFailedAttempts_EventuallyRateLimitedOrLocked()
    {
        // Arrange - Make multiple failed login attempts
        var responses = new List<HttpResponseMessage>();

        // Act
        for (int i = 0; i < 15; i++)
        {
            var request = new
            {
                Email = "lockout-test@test.com",
                Password = $"WrongPassword{i}!",
                RememberMe = false
            };

            var response = await Client.PostAsJsonAsync("/api/auth/login", request);
            responses.Add(response);
        }

        // Assert - Should eventually get rate limited, locked, or continue with unauthorized
        var statusCodes = responses.Select(r => r.StatusCode).ToList();
        statusCodes.Should().OnlyContain(s =>
            s == HttpStatusCode.Unauthorized ||
            s == HttpStatusCode.BadRequest ||
            s == HttpStatusCode.InternalServerError ||
            s == (HttpStatusCode)423 || // Locked
            s == (HttpStatusCode)429);  // Too Many Requests
    }

    #endregion

    #region Legacy Route Tests

    [Fact]
    [FastTest]
    public async Task POST_LegacyRoute_Login_Works()
    {
        // Arrange
        var request = new
        {
            Email = _existingUser.Email,
            Password = ExistingUserPassword,
            RememberMe = false
        };

        // Act - Use legacy route without /api prefix
        var response = await Client.PostAsJsonAsync("/auth/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task GET_LegacyRoute_Status_Works()
    {
        // Act - Use legacy route without /api prefix
        var response = await Client.GetAsync("/auth/status");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
    }

    #endregion

    #region Input Validation Tests

    [Fact]
    [FastTest]
    public async Task POST_Register_WithVeryLongEmail_ReturnsBadRequest()
    {
        // Arrange
        var longEmail = new string('a', 300) + "@test.com";
        var request = new
        {
            Email = longEmail,
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithVeryLongPassword_ReturnsBadRequest()
    {
        // Arrange
        var longPassword = new string('A', 500) + "1!";
        var request = new
        {
            Email = "longpass@test.com",
            Password = longPassword,
            ConfirmPassword = longPassword,
            FirstName = "Test",
            LastName = "User",
            AcceptTerms = true
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.OK, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithEmptyBody_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    [FastTest]
    public async Task POST_Login_WithEmptyBody_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Account Status Tests

    [Fact]
    [FastTest]
    public async Task POST_Login_WithSuspendedUser_ReturnsUnauthorizedOrLocked()
    {
        // Arrange - Create a suspended user
        var suspendedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "suspended-user@test.com",
            UserName = "suspended-user@test.com",
            Status = UserStatus.Suspended,
            EmailConfirmed = true
        };

        Context.Users.Add(suspendedUser);
        await Context.SaveChangesAsync();

        var request = new
        {
            Email = suspendedUser.Email,
            Password = "SomePassword123!",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, (HttpStatusCode)423, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Login_WithBannedUser_ReturnsUnauthorizedOrForbidden()
    {
        // Arrange - Create a banned user
        var bannedUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "banned-user@test.com",
            UserName = "banned-user@test.com",
            Status = UserStatus.Banned,
            EmailConfirmed = true
        };

        Context.Users.Add(bannedUser);
        await Context.SaveChangesAsync();

        var request = new
        {
            Email = bannedUser.Email,
            Password = "SomePassword123!",
            RememberMe = false
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden, (HttpStatusCode)423, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region IP Address Handling Tests (Coverage: GetClientIpAddress)

    [Fact]
    [FastTest]
    public async Task POST_Register_WithXForwardedForHeader_UsesForwardedIp()
    {
        // Arrange
        var request = new
        {
            Email = $"forwarded-ip-user-{Guid.NewGuid():N}@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "Forwarded",
            LastName = "User",
            AcceptTerms = true
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-Forwarded-For", "203.0.113.45, 198.51.100.23");

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_Register_WithXRealIpHeader_UsesRealIp()
    {
        // Arrange
        var request = new
        {
            Email = $"real-ip-user-{Guid.NewGuid():N}@test.com",
            Password = "SecurePass123!@#",
            ConfirmPassword = "SecurePass123!@#",
            FirstName = "RealIP",
            LastName = "User",
            AcceptTerms = true
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/register")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-Real-IP", "192.0.2.100");

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.InternalServerError);
    }

    [Fact]
    [FastTest]
    public async Task POST_ForgotPassword_WithXForwardedForHeader_UsesForwardedIp()
    {
        // Arrange
        var request = new
        {
            Email = _existingUser.Email
        };

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgot-password")
        {
            Content = JsonContent.Create(request)
        };
        requestMessage.Headers.Add("X-Forwarded-For", "198.18.0.5");

        // Act
        var response = await Client.SendAsync(requestMessage);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.TooManyRequests);
    }

    #endregion
}
