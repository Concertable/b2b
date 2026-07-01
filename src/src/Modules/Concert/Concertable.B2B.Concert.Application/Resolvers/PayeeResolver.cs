using System.Collections.Frozen;
using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class PayeeResolver : IPayeeResolver
{
    private readonly FrozenDictionary<ContractType, IPayeeResolver> resolvers;

    public PayeeResolver(VenuePayeeResolver venue, ArtistPayeeResolver artist)
    {
        resolvers = new Dictionary<ContractType, IPayeeResolver>
        {
            [ContractType.FlatFee] = venue,
            [ContractType.DoorSplit] = venue,
            [ContractType.Versus] = venue,
            [ContractType.VenueHire] = artist,
        }.ToFrozenDictionary();
    }

    public Guid ResolveUserId(ConcertEntity concert) =>
        resolvers[concert.ContractType].ResolveUserId(concert);

    public Guid ResolveTenantId(ConcertEntity concert) =>
        resolvers[concert.ContractType].ResolveTenantId(concert);
}
