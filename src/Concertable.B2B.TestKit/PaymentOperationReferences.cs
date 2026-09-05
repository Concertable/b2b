using System.Globalization;

namespace Concertable.B2B.TestKit;

public static class PaymentOperationReferences
{
    public const string EscrowType = "escrow";
    public const string SettlementType = "settlement";

    public static string EscrowClientReference(int bookingId) =>
        "booking:" + bookingId.ToString(CultureInfo.InvariantCulture);

    public static string SettlementClientReference(int concertId) =>
        "concert:" + concertId.ToString(CultureInfo.InvariantCulture);
}
