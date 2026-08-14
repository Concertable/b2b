using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Artist.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<ArtistTenantDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(ArtistTenantDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<ArtistTenantDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
