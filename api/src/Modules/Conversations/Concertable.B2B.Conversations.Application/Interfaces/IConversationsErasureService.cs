namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationsErasureService
{
    Task SeverAuthoredMessagesAsync(Guid userId, CancellationToken ct = default);

    Task ScrubParticipantProfilesAsync(IReadOnlySet<Guid> tenantIds, CancellationToken ct = default);
}
