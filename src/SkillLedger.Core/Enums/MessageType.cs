namespace SkillLedger.Core.Enums
{
    /// <summary>
    /// Types of messages that can be sent in a workspace
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// Regular text message
        /// </summary>
        Text = 0,

        /// <summary>
        /// File attachment message
        /// </summary>
        File = 1,

        /// <summary>
        /// System-generated message (e.g., user joined, milestone completed)
        /// </summary>
        System = 2,

        /// <summary>
        /// Milestone or deliverable update message
        /// </summary>
        Milestone = 3,

        /// <summary>
        /// Image attachment message
        /// </summary>
        Image = 4,

        /// <summary>
        /// Voice message attachment
        /// </summary>
        Voice = 5
    }
}