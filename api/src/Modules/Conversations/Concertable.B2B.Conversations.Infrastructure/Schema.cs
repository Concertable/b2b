namespace Concertable.B2B.Conversations.Infrastructure;

internal static class Schema
{
    public const string Name = "conversations";

    public static class Tables
    {
        public const string ContentReports = "ContentReports";
        public const string Conversations = "Conversations";
        public const string ConversationAccessGrants = "ConversationAccessGrants";
        public const string ConversationCreationReceipts = "ConversationCreationReceipts";
        public const string Messages = "Messages";
        public const string ConversationReadPositions = "ConversationReadPositions";
        public const string TenantDisplays = "TenantDisplays";
    }
}
