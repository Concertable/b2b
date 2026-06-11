using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Resolvers;

internal sealed class ArtistPayeeResolver : IPayeeResolver
{
    public Guid ResolveUserId(ConcertEntity concert) => concert.Artist.UserId;
    public Guid ResolveTenantId(ConcertEntity concert) => concert.ArtistTenantId;
}
