using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal sealed class MessagePrivilegedRepository : Repository<MessageEntity, int>, IMessagePrivilegedRepository
{
    private readonly ConversationsPrivilegedDbContext context;

    public MessagePrivilegedRepository(ConversationsPrivilegedDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<MessageEntity>> ListBySenderUserAsync(Guid userId, CancellationToken ct = default) =>
        await context.Messages.Where(m => m.SentByUserId == userId).ToListAsync(ct);
}
