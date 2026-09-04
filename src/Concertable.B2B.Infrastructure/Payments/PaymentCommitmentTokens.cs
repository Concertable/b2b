namespace Concertable.B2B.Infrastructure.Payments;

/// <summary>
/// The operation-type half of a Payment operation reference. Application mints these, Booking confirms
/// against them and Concert settles with them, so the three modules must agree on the exact strings —
/// which is why they live here rather than in any one module.
/// </summary>
public static class PaymentCommitmentTokens
{
    public const string EscrowHold = "escrow-hold";
    public const string MethodSetup = "method-setup";
    public const string MethodVerification = "method-verification";
}
