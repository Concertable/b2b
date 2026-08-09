using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class VenuePaysArtistDealPayeeResolver : IDealPayeeResolver
{
    public Guid ResolveTicketUserId(ConcertEntity concert) => concert.Venue.UserId;

    public Guid ResolveTicketTenantId(ConcertEntity concert) => concert.VenueTenantId;

    public Guid ResolveSettlementTenantId(ConcertEntity concert) => concert.ArtistTenantId;
}
