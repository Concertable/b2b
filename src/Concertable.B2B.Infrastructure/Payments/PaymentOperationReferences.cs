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

    private const string ApplicationPrefix = "app:";
    private const string BookingPrefix = "booking:";
    private const string ConcertPrefix = "concert:";

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

    public static bool TryReadApplicationId(PaymentOperationReference reference, out int applicationId) =>
        TryRead(reference.ClientReference, ApplicationPrefix, out applicationId);

    public static bool TryReadBookingId(PaymentOperationReference reference, out int bookingId) =>
        TryRead(reference.ClientReference, BookingPrefix, out bookingId);

    public static bool TryReadConcertId(PaymentOperationReference reference, out int concertId) =>
        TryRead(reference.ClientReference, ConcertPrefix, out concertId);

    // Payment's E2E surface addresses an operation by its two halves rather than the value object, so the
    // format still lives here and only the composed string leaves.
    public static string EscrowClientReference(int bookingId) => Escrow(bookingId).ClientReference;

    public static string SettlementClientReference(int concertId) => Settlement(concertId).ClientReference;

    public static int ReadApplicationId(PaymentOperationReference reference) =>
        TryReadApplicationId(reference, out var applicationId)
            ? applicationId
            : throw new InvalidOperationException(
                $"Payment operation {reference.ClientReference} does not name an application.");

    public static int ReadConcertId(PaymentOperationReference reference) =>
        TryReadConcertId(reference, out var concertId)
            ? concertId
            : throw new InvalidOperationException(
                $"Settlement operation {reference.ClientReference} does not name a concert.");

    public static int ReadBookingId(PaymentOperationReference reference) =>
        TryReadBookingId(reference, out var bookingId)
            ? bookingId
            : throw new InvalidOperationException(
                $"Escrow operation {reference.ClientReference} does not name a booking.");

    private static string ForApplication(int applicationId) =>
        ApplicationPrefix + applicationId.ToString(CultureInfo.InvariantCulture);

    private static bool TryRead(string clientReference, string prefix, out int id)
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
