using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class ArtistPaysVenueDealPayeeResolver : IDealPayeeResolver
{
    public Guid ResolveTicketUserId(ConcertEntity concert) => concert.Artist.UserId;

    public Guid ResolveTicketTenantId(ConcertEntity concert) => concert.ArtistTenantId;

    public Guid ResolveSettlementTenantId(ConcertEntity concert) => concert.VenueTenantId;
}
