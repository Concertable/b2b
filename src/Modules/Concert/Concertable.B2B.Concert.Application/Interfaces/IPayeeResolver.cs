using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Resolves one side (user + tenant) of a concert — the generic leaf the payee facades are built from, blind to
/// which payment is in play. The two concrete leaves are <c>VenuePayeeResolver</c> / <c>ArtistPayeeResolver</c>;
/// the deal→side mapping lives in the facades — <see cref="ITicketPayeeResolver"/> (who collects ticket revenue)
/// and <see cref="ISettlementPayeeResolver"/> (who receives the settlement) — each a keyed strategy resolver over
/// these leaves. Consumers never branch on deal type.
/// </summary>
internal interface IPayeeResolver
{
    Guid ResolveUserId(ConcertEntity concert);
    Guid ResolveTenantId(ConcertEntity concert);
}
