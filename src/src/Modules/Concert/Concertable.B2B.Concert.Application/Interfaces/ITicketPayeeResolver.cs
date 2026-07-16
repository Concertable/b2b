using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Resolves who collects a concert's <em>ticket revenue</em> — the party the ticket money is paid to (published on
/// <c>ConcertChangedEvent</c>). Direction-dependent by deal type: the venue for revenue-share/fixed-fee, the artist
/// for VenueHire. The exact inverse of <see cref="ISettlementPayeeResolver"/> (who receives the settlement); kept a
/// distinct facade so each reads its own recipient directly. A keyed strategy resolver over the
/// <see cref="IPayeeResolver"/> leaves; consumers never branch on deal type.
/// </summary>
internal interface ITicketPayeeResolver
{
    Guid ResolveUserId(ConcertEntity concert);
    Guid ResolveTenantId(ConcertEntity concert);
}
