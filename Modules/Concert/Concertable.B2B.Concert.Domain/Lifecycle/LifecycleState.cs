namespace Concertable.B2B.Concert.Domain.Lifecycle;

public enum LifecycleState
{
    Applied,
    Rejected,
    Withdrawn,
    Accepted,               // accept landed; payment leg pending (which leg = contract type)
    PaymentFailed,          // accept-leg payment failed (verify hold / escrow capture) â€” retryable
    Booked,                 // payment confirmed, draft created â€” CanPost gate
    AwaitingSettlement,     // deferred payout leg
    SettlementFailed,       // post-Finish payout failed â€” recovery lands Complete, not Booked
    Complete,
}
