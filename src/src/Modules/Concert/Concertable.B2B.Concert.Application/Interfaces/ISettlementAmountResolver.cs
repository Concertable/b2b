using Concertable.B2B.Deal.Contracts;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The single source of truth for a settlement's gross (VAT-inclusive) consideration, keyed by
/// <see cref="IDeal.DealType"/>: the fixed fee for FlatFee/VenueHire, the venue-declared revenue share for
/// DoorSplit/Versus. Both the payout step and the invoice issuer resolve through this so the charged and
/// invoiced amounts can never diverge.
/// </summary>
internal interface ISettlementAmountResolver
{
    Task<decimal> ResolveGrossAsync(int concertId, IDeal deal, CancellationToken ct = default);
}
