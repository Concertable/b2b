using Concertable.B2B.Conversations.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessageReadRepository : IMessageReadRepository
{
    private readonly IConversationsReadDbContext context;

    public MessageReadRepository(IConversationsReadDbContext context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<MessageEntity>> ListBySenderUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Messages.Where(m => m.SentByUserId != null && m.SentByUserId == userId).ToListAsync(ct);
}
