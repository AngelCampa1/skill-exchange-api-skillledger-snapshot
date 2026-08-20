using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SkillLedger.Core.Interfaces;
using System.Security.Claims;

namespace SkillLedger.Api.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time financial analytics and reporting
    /// Handles live dashboard updates, credit balance changes, and transaction notifications
    /// </summary>
    [Authorize]
    public class FinancialAnalyticsHub : Hub
    {
        private readonly IFinancialReportingService _financialReportingService;
        private readonly ICreditWalletService _creditWalletService;
        private readonly ILogger<FinancialAnalyticsHub> _logger;

        public FinancialAnalyticsHub(
            IFinancialReportingService financialReportingService,
            ICreditWalletService creditWalletService,
            ILogger<FinancialAnalyticsHub> logger)
        {
            _financialReportingService = financialReportingService;
            _creditWalletService = creditWalletService;
            _logger = logger;
        }

        /// <summary>
        /// Subscribes user to their personal analytics updates
        /// </summary>
        public async Task SubscribeToPersonalAnalyticsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                _logger.LogWarning("User ID not found in claims for analytics subscription");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_analytics_{userId}");

            _logger.LogInformation("User {UserId} subscribed to personal analytics via connection {ConnectionId}",
                userId, Context.ConnectionId);

            // Send current dashboard data immediately
            try
            {
                var dashboardData = await _financialReportingService.GetUserDashboardDataAsync(userId.Value);
                await Clients.Caller.SendAsync("DashboardDataUpdate", dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send initial dashboard data for user {UserId}", userId);
            }
        }

        /// <summary>
        /// Unsubscribes user from their personal analytics updates
        /// </summary>
        public async Task UnsubscribeFromPersonalAnalyticsAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_analytics_{userId}");

            _logger.LogInformation("User {UserId} unsubscribed from personal analytics via connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        /// <summary>
        /// Subscribes admin user to system-wide analytics (admin only)
        /// </summary>
        public async Task SubscribeToSystemAnalyticsAsync()
        {
            if (!IsAdmin())
            {
                _logger.LogWarning("Non-admin user {UserId} attempted to subscribe to system analytics", GetCurrentUserId());
                await Clients.Caller.SendAsync("Error", "Unauthorized access to system analytics");
                return;
            }

            var userId = GetCurrentUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, "system_analytics");

            _logger.LogInformation("Admin user {UserId} subscribed to system analytics via connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        /// <summary>
        /// Unsubscribes admin user from system-wide analytics
        /// </summary>
        public async Task UnsubscribeFromSystemAnalyticsAsync()
        {
            if (!IsAdmin())
            {
                return;
            }

            var userId = GetCurrentUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "system_analytics");

            _logger.LogInformation("Admin user {UserId} unsubscribed from system analytics via connection {ConnectionId}",
                userId, Context.ConnectionId);
        }

        /// <summary>
        /// Requests current wallet balance (triggers real-time update)
        /// </summary>
        public async Task RequestWalletBalanceAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return;
            }

            try
            {
                var wallet = await _creditWalletService.GetWalletAsync(userId.Value);
                if (wallet != null)
                {
                    await Clients.Caller.SendAsync("WalletBalanceUpdate", new
                    {
                        Balance = wallet.Balance,
                        EncryptedBalance = wallet.EncryptedBalance,
                        UpdatedAt = wallet.UpdatedAt
                    });
                }
                else
                {
                    _logger.LogWarning("Wallet not found for user {UserId}", userId);
                    await Clients.Caller.SendAsync("Error", "Wallet not found");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve wallet balance for user {UserId}", userId);
                await Clients.Caller.SendAsync("Error", "Failed to retrieve wallet balance");
            }
        }

        /// <summary>
        /// Called when a client connects to the hub
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            _logger.LogInformation("User {UserId} connected to FinancialAnalyticsHub with connection {ConnectionId}",
                userId, Context.ConnectionId);

            // Auto-subscribe to personal analytics on connection
            if (userId.HasValue)
            {
                await SubscribeToPersonalAnalyticsAsync();
            }

            await base.OnConnectedAsync();
        }

        /// <summary>
        /// Called when a client disconnects from the hub
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();

            _logger.LogInformation("User {UserId} disconnected from FinancialAnalyticsHub with connection {ConnectionId}. Exception: {Exception}",
                userId, Context.ConnectionId, exception?.Message);

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Gets the current user's ID from the JWT claims
        /// </summary>
        /// <returns>User ID or null if not found</returns>
        private Guid? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Checks if the current user is an admin
        /// </summary>
        /// <returns>True if user is admin, false otherwise</returns>
        private bool IsAdmin()
        {
            return Context.User?.IsInRole("Admin") == true;
        }
    }
}
