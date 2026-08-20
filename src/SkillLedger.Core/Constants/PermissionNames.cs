namespace SkillLedger.Core.Constants;

/// <summary>
/// Standard permission names used throughout the application
/// </summary>
public static class PermissionNames
{
    #region User Management
    public const string ViewUsers = "VIEW_USERS";
    public const string CreateUsers = "CREATE_USERS";
    public const string EditUsers = "EDIT_USERS";
    public const string DeleteUsers = "DELETE_USERS";
    public const string ManageUserRoles = "MANAGE_USER_ROLES";
    #endregion

    #region Project Management
    public const string ViewProjects = "VIEW_PROJECTS";
    public const string CreateProjects = "CREATE_PROJECTS";
    public const string CREATE_PROJECT = "CREATE_PROJECT"; // Legacy constant for backward compatibility
    public const string EditProjects = "EDIT_PROJECTS";
    public const string DeleteProjects = "DELETE_PROJECTS";
    public const string ManageProjectParticipants = "MANAGE_PROJECT_PARTICIPANTS";
    #endregion

    #region Escrow Management
    public const string ViewEscrow = "VIEW_ESCROW";
    public const string CreateEscrow = "CREATE_ESCROW";
    public const string ManageEscrow = "MANAGE_ESCROW";
    public const string ReleaseEscrow = "RELEASE_ESCROW";
    public const string DisputeEscrow = "DISPUTE_ESCROW";
    public const string ADMIN_ESCROW_MANAGEMENT = "ADMIN_ESCROW_MANAGEMENT";
    public const string ADMIN_SYSTEM_METRICS = "ADMIN_SYSTEM_METRICS";
    #endregion

    #region Credit System
    public const string ViewCredits = "VIEW_CREDITS";
    public const string TransferCredits = "TRANSFER_CREDITS";
    public const string ManageCredits = "MANAGE_CREDITS";
    public const string ViewTransactionHistory = "VIEW_TRANSACTION_HISTORY";
    #endregion

    #region System Administration
    public const string ViewSystemLogs = "VIEW_SYSTEM_LOGS";
    public const string ManageSystemSettings = "MANAGE_SYSTEM_SETTINGS";
    public const string ViewAnalytics = "VIEW_ANALYTICS";
    public const string ManageRoles = "MANAGE_ROLES";
    public const string ManagePermissions = "MANAGE_PERMISSIONS";
    #endregion

    #region Content Moderation
    public const string ModerateContent = "MODERATE_CONTENT";
    public const string ViewReports = "VIEW_REPORTS";
    public const string ManageReports = "MANAGE_REPORTS";
    #endregion

    #region Support
    public const string ViewSupportTickets = "VIEW_SUPPORT_TICKETS";
    public const string ManageSupportTickets = "MANAGE_SUPPORT_TICKETS";
    public const string AccessSupportTools = "ACCESS_SUPPORT_TOOLS";
    #endregion

    #region Tax Compliance
    public const string ViewTaxInformation = "VIEW_TAX_INFORMATION";
    public const string ManageTaxInformation = "MANAGE_TAX_INFORMATION";
    public const string VerifyTaxInformation = "VERIFY_TAX_INFORMATION";
    public const string GenerateTaxDocuments = "GENERATE_TAX_DOCUMENTS";
    public const string ViewTaxReports = "VIEW_TAX_REPORTS";
    #endregion

    /// <summary>
    /// All permissions grouped by category
    /// </summary>
    public static readonly Dictionary<string, string[]> ByCategory = new()
    {
        ["User Management"] = new[]
        {
            ViewUsers, CreateUsers, EditUsers, DeleteUsers, ManageUserRoles
        },
        ["Project Management"] = new[]
        {
            ViewProjects, CreateProjects, CREATE_PROJECT, EditProjects, DeleteProjects, ManageProjectParticipants
        },
        ["Escrow Management"] = new[]
        {
            ViewEscrow, CreateEscrow, ManageEscrow, ReleaseEscrow, DisputeEscrow, ADMIN_ESCROW_MANAGEMENT, ADMIN_SYSTEM_METRICS
        },
        ["Credit System"] = new[]
        {
            ViewCredits, TransferCredits, ManageCredits, ViewTransactionHistory
        },
        ["System Administration"] = new[]
        {
            ViewSystemLogs, ManageSystemSettings, ViewAnalytics, ManageRoles, ManagePermissions
        },
        ["Content Moderation"] = new[]
        {
            ModerateContent, ViewReports, ManageReports
        },
        ["Support"] = new[]
        {
            ViewSupportTickets, ManageSupportTickets, AccessSupportTools
        },
        ["Tax Compliance"] = new[]
        {
            ViewTaxInformation, ManageTaxInformation, VerifyTaxInformation,
            GenerateTaxDocuments, ViewTaxReports
        }
    };

    /// <summary>
    /// All permissions in a flat array
    /// </summary>
    public static readonly string[] All = ByCategory.Values.SelectMany(x => x).ToArray();
}