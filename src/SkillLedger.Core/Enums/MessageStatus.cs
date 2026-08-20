namespace SkillLedger.Core.Enums
{
    /// <summary>
    /// Status of a message for read receipts and delivery tracking
    /// </summary>
    public enum MessageStatus
    {
        /// <summary>
        /// Message sent but not yet delivered to recipient
        /// </summary>
        Sent = 0,

        /// <summary>
        /// Message delivered to recipient's device
        /// </summary>
        Delivered = 1,

        /// <summary>
        /// Message read by recipient
        /// </summary>
        Read = 2,

        /// <summary>
        /// Message failed to send
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Message deleted by sender
        /// </summary>
        Deleted = 4
    }
}