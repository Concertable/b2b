using System.Globalization;
using Concertable.Payment.Contracts;

namespace Concertable.B2B.Infrastructure.Payments;

/// <summary>
/// The Payment operation references B2B names. Both halves of every reference are frozen vocabulary:
/// changing either strands the operations Payment has already indexed under the old value.
/// </summary>
public static class PaymentOperationReferences
{
    public const string EscrowHoldType = "escrow-hold";
    public const string MethodSetupType = "method-setup";
    public const string EscrowType = "escrow";
    public const string SettlementType = "settlement";

    // Payment stamps the operation type as the provider object's `type` metadata and its setup-intent
    // webhook only publishes an outcome for `verify`, so the verification operation must carry Payment's
    // own constant or B2B never hears that the card was confirmed.
    public const string MethodVerificationType = TransactionTypes.Verify;

    internal const string ApplicationPrefix = "app:";
    internal const string BookingPrefix = "booking:";
    internal const string ConcertPrefix = "concert:";

    public static PaymentOperationReference EscrowHold(int applicationId) =>
        new(EscrowHoldType, ForApplication(applicationId));

    // The artist commits their method before the application row exists, so this one is keyed by the
    // opportunity and the artist. Apply checkout, the apply-time validation and the frozen contract
    // snapshot all compose it and must produce an identical string.
    public static PaymentOperationReference MethodSetup(int opportunityId, Guid artistTenantId) =>
        new(MethodSetupType, $"opp:{opportunityId.ToString(CultureInfo.InvariantCulture)}:artist:{artistTenantId}");

    public static PaymentOperationReference MethodVerification(int applicationId) =>
        new(MethodVerificationType, ForApplication(applicationId));

    public static PaymentOperationReference Escrow(int bookingId) =>
        new(EscrowType, BookingPrefix + bookingId.ToString(CultureInfo.InvariantCulture));

    public static PaymentOperationReference Settlement(int concertId) =>
        new(SettlementType, ConcertPrefix + concertId.ToString(CultureInfo.InvariantCulture));

    private static string ForApplication(int applicationId) =>
        ApplicationPrefix + applicationId.ToString(CultureInfo.InvariantCulture);
}

public static class PaymentOperationReferenceExtensions
{
    extension(PaymentOperationReference reference)
    {
        public bool TryGetApplicationId(out int applicationId) =>
            TryGet(reference.ClientReference, PaymentOperationReferences.ApplicationPrefix, out applicationId);

        public bool TryGetBookingId(out int bookingId) =>
            TryGet(reference.ClientReference, PaymentOperationReferences.BookingPrefix, out bookingId);

        public bool TryGetConcertId(out int concertId) =>
            TryGet(reference.ClientReference, PaymentOperationReferences.ConcertPrefix, out concertId);
    }

    private static bool TryGet(string clientReference, string prefix, out int id)
    {
        id = 0;
        return clientReference.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(
                clientReference.AsSpan(prefix.Length),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out id);
    }
}
