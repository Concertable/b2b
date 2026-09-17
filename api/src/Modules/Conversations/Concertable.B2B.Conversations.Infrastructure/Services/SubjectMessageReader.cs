using Concertable.B2B.Conversations.Infrastructure.Mappers;

namespace Concertable.B2B.Conversations.Infrastructure.Services;

internal sealed class SubjectMessageReader : ISubjectMessageReader
{
    private readonly IMessageReadRepository messages;

    public SubjectMessageReader(IMessageReadRepository messages)
    {
        this.messages = messages;
    }

    public async Task<IReadOnlyList<SubjectMessageDto>> GetSubjectMessagesAsync(Guid userId, CancellationToken ct = default)
    {
        var authored = await messages.ListBySenderUserAsync(userId, ct);
        return authored.Select(m => m.ToSubjectMessageDto()).ToList();
    }
}
