using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class SettlementPayeeResolver : ISettlementPayeeResolver
{
    /* The settlement recipient by deal type: the venue pays the artist for revenue-share and fixed-fee, the
       artist pays the venue for VenueHire. The leaves are the same role-extractors the TicketPayeeResolver
       uses — only the deal→party mapping differs (this one is its inverse). */
    private readonly FrozenDictionary<DealType, IPayeeResolver> resolvers;

    public SettlementPayeeResolver(VenuePayeeResolver venue, ArtistPayeeResolver artist)
    {
        resolvers = new Dictionary<DealType, IPayeeResolver>
        {
            [DealType.FlatFee] = artist,
            [DealType.DoorSplit] = artist,
            [DealType.Versus] = artist,
            [DealType.VenueHire] = venue,
        }.ToFrozenDictionary();
    }

    public Guid ResolveTenantId(ConcertEntity concert) =>
        resolvers[concert.DealType].ResolveTenantId(concert);
}
