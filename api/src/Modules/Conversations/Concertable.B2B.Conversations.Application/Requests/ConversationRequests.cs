namespace Concertable.B2B.Conversations.Application.Requests;

internal sealed record CreateConversationRequest(
    Guid RequestId,
    IReadOnlyList<Guid> ParticipantTenantIds);

internal sealed record SendMessageRequest(Guid RequestId, string Content);

internal sealed record AdvanceConversationReadPositionRequest(long ThroughSequence);

internal sealed record AssignConversationMemberRequest(Guid MembershipId, long ExpectedAccessVersion);
