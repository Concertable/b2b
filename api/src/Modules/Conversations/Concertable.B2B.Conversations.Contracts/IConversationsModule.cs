namespace Concertable.B2B.Conversations.Contracts;

public interface IConversationsModule
{
    /// <summary>
    /// Sends into the conversation between exactly <paramref name="participantTenantIds"/>, opening one if
    /// there is none. The participants are a set rather than a fixed pair because a conversation can involve
    /// a business that is neither the venue nor the performer.
    /// </summary>
    Task SendAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null);

    /// <inheritdoc cref="SendAsync"/>
    /// <remarks>Also pushes the message to every member of the other participants.</remarks>
    Task SendAndNotifyAsync(
        IReadOnlyCollection<Guid> participantTenantIds,
        Guid senderTenantId,
        Guid sentByUserId,
        string content,
        MessageAction? action = null);
}
