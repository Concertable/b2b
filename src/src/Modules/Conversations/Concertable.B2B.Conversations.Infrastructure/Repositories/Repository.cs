using Concertable.B2B.Conversations.Infrastructure.Data;
using Concertable.DataAccess.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Repositories;

internal abstract class Repository<TEntity>(ConversationsDbContext context)
    : Repository<TEntity, ConversationsDbContext, int>(context)
    where TEntity : class, IIdEntity;
