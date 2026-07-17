using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Resolves the tenant a concert's settlement <em>pays</em> — the seller who receives the money. This is
/// direction-dependent by deal type: the artist for revenue-share and fixed-fee (FlatFee/DoorSplit/Versus, where
/// the venue pays the artist), the venue for VenueHire (the artist hired the venue). Deliberately a distinct rule
/// from <see cref="ITicketPayeeResolver"/> — which resolves the <em>ticket-revenue</em> collector and is its exact
/// inverse — so the payout gate reads the settlement recipient directly rather than inverting an unrelated concept.
/// A keyed strategy resolver over the <see cref="IPayeeResolver"/> leaves; consumers never branch on deal type.
/// </summary>
internal interface ISettlementPayeeResolver
{
    Guid ResolveTenantId(ConcertEntity concert);
}
