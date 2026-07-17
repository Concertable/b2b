using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class SettlementPayeeResolver : ISettlementPayeeResolver
{
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
