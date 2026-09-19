using Concertable.B2B.Conversations.Application.Errors;
using Concertable.B2B.Conversations.Application.Requests;
using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Application.Interfaces;

internal interface IConversationService
{
    Task<Result<ConversationDto, CreateConversationError>> CreateAsync(
        CreateConversationRequest request,
        CancellationToken ct = default);

    Task<Result<ConversationDto, ConversationAccessError>> GetAsync(
        int conversationId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<MessageDto>, ConversationAccessError>> GetMessagesAsync(
        int conversationId,
        CancellationToken ct = default);

    Task<Result<MessageDto, SendMessageError>> SendAsync(
        int conversationId,
        SendMessageRequest request,
        MessageAction? action = null,
        CancellationToken ct = default);

    Task<UnitResult<ConversationAccessError>> AdvanceReadPositionAsync(
        int conversationId,
        AdvanceConversationReadPositionRequest request,
        CancellationToken ct = default);

    Task<UnitResult<AssignConversationMemberError>> AssignMemberAsync(
        int conversationId,
        AssignConversationMemberRequest request,
        CancellationToken ct = default);

    Task<UnitResult<AssignConversationMemberError>> RemoveMemberAssignmentAsync(
        int conversationId,
        Guid membershipId,
        CancellationToken ct = default);

    Task<int> GetUnreadCountAsync(CancellationToken ct = default);
    Task<IReadOnlyList<MessagePreviewDto>> GetRecentPreviewsAsync(CancellationToken ct = default);
}
