using Concertable.B2B.Artist.Infrastructure.Data;
using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure;
using Concertable.Messaging.Infrastructure.Outbox;

namespace Concertable.B2B.Artist.Infrastructure;

internal interface IOutboxUnitOfWorkBehavior : IOutboxUnitOfWorkBehavior<TenantArtistDbContext>;

internal sealed class OutboxUnitOfWorkBehavior(TenantArtistDbContext context, IDbContextAccessor accessor)
    : OutboxUnitOfWorkBehavior<TenantArtistDbContext>(context, accessor), IOutboxUnitOfWorkBehavior;
