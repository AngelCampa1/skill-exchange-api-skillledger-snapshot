namespace SkillLedger.Core.Enums
{
    /// <summary>
    /// Defines the permission levels for document sharing
    /// </summary>
    public enum SharePermission
    {
        /// <summary>
        /// Can only view the document
        /// </summary>
        View = 1,

        /// <summary>
        /// Can view and download the document
        /// </summary>
        Download = 2,

        /// <summary>
        /// Can view, download, and edit the document
        /// </summary>
        Edit = 3,

        /// <summary>
        /// Full access including sharing with others and deletion
        /// </summary>
        Admin = 4
    }
}