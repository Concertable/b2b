namespace Concertable.B2B.Conversations.Infrastructure;

internal static class Schema
{
    public const string Name = "conversations";

    public static class Tables
    {
        public const string ContentReports = "ContentReports";
        public const string Messages = "Messages";
        public const string ThreadReadStates = "ThreadReadStates";
        public const string ParticipantProfiles = "ParticipantProfiles";
    }
}
