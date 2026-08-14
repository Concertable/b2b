using Concertable.B2B.Conversations.Application.DTOs;

namespace Concertable.B2B.Conversations.Api.Responses;

internal sealed record MessageResponse
{
    public int Id { get; init; }
    public required Guid CounterpartTenantId { get; init; }
    public required MessageSender Sender { get; init; }
    public MessageAction? Action { get; init; }
    public required string Content { get; init; }
    public required MessageActions Actions { get; init; }
}

internal sealed record MessageActions(ActionLink? Report);
