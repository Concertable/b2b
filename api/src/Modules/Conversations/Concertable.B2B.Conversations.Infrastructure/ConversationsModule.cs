using Concertable.B2B.Conversations.Application.Requests;

namespace Concertable.B2B.Conversations.Infrastructure;

internal sealed class ConversationsModule : IConversationsModule
{
    private readonly IConversationService conversationService;

    public ConversationsModule(IConversationService conversationService)
    {
        this.conversationService = conversationService;
    }

    public async Task<int> CreateAsync(
        Guid requestId,
        IReadOnlyCollection<Guid> participantTenantIds,
        CancellationToken ct = default)
    {
        var result = await conversationService.CreateAsync(
            new CreateConversationRequest(requestId, [.. participantTenantIds]), ct);
        return result.TryGetValue(out var conversation)
            ? conversation.ConversationId
            : throw new InvalidOperationException("The conversation could not be created.");
    }

    public async Task SendAsync(
        int conversationId,
        Guid requestId,
        string content,
        MessageAction? action = null,
        CancellationToken ct = default)
    {
        var result = await conversationService.SendAsync(
            conversationId,
            new SendMessageRequest(requestId, content),
            action,
            ct);
        if (result.TryGetError(out _))
            throw new InvalidOperationException("The conversation message could not be sent.");
    }
}
