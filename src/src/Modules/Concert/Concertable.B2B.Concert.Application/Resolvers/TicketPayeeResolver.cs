using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class TicketPayeeResolver : ITicketPayeeResolver
{
    private readonly FrozenDictionary<DealType, IPayeeResolver> resolvers;

    public TicketPayeeResolver(VenuePayeeResolver venue, ArtistPayeeResolver artist)
    {
        resolvers = new Dictionary<DealType, IPayeeResolver>
        {
            [DealType.FlatFee] = venue,
            [DealType.DoorSplit] = venue,
            [DealType.Versus] = venue,
            [DealType.VenueHire] = artist,
        }.ToFrozenDictionary();
    }

    public Guid ResolveUserId(ConcertEntity concert) =>
        resolvers[concert.DealType].ResolveUserId(concert);

    public Guid ResolveTenantId(ConcertEntity concert) =>
        resolvers[concert.DealType].ResolveTenantId(concert);
}
