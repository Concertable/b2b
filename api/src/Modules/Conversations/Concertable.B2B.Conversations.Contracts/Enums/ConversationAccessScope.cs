namespace Concertable.B2B.Conversations.Contracts.Enums;

/// <summary>
/// What a grant on a conversation lets a tenant do. Reading a conversation and being able to add to it are
/// separate: a business given sight of a conversation does not thereby get to speak in it.
/// </summary>
public enum ConversationAccessScope
{
    Read = 1,
    SendMessages = 2,
}
