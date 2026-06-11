using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class VenuePayeeResolver : IPayeeResolver
{
    public Guid ResolveUserId(ConcertEntity concert) => concert.Venue.UserId;
    public Guid ResolveTenantId(ConcertEntity concert) => concert.VenueTenantId;
}
