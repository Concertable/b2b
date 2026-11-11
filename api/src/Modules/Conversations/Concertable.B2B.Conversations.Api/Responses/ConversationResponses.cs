using Concertable.B2B.Conversations.Application.DTOs;
using Concertable.B2B.Conversations.Contracts.Enums;

namespace Concertable.B2B.Conversations.Api.Responses;

internal sealed record ConversationResponse(
    int ConversationId,
    long AccessVersion,
    IReadOnlyList<ConversationParticipant> Participants);

internal sealed record MessageResponse(
    int Id,
    int ConversationId,
    long Sequence,
    Guid SenderTenantId,
    Guid SentByUserId,
    string Content,
    DateTime SentAt,
    MessageAction? Action,
    MessageActions Actions);

internal sealed record MessageActions(ActionLink? Report);
