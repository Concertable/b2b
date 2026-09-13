using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// The single source of truth for a settlement's gross (VAT-inclusive) consideration, keyed by
/// <see cref="DealDto.DealType"/>: the fixed fee for FlatFee/VenueHire, the venue-declared revenue share for
/// DoorSplit/Versus. Both the payout step and the invoice issuer resolve through this so the charged and
/// invoiced amounts can never diverge.
/// </summary>
internal interface ISettlementAmountResolver : IDealStrategy
{
    Task<Money> ResolveGrossAsync(int concertId, DealDto deal, CancellationToken ct = default);
}
