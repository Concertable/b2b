namespace Concertable.B2B.Conversations.Contracts.Enums;

/// <summary>
/// What a grant on a thread lets a tenant do. Reading a conversation and being able to add to it are
/// separate: a business given sight of a thread does not thereby get to speak in it.
/// </summary>
public enum ThreadAccessScope
{
    Read = 1,
    Participate = 2,
}
