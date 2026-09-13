using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.ValueObjects;
using static System.FormattableString;

namespace Concertable.B2B.Application.Domain;

internal static class ApplicationTermsFingerprint
{
    public static string Calculate(DealDto deal, DateRange period)
    {
        var numbers = deal switch
        {
            FlatFeeDealDto flatFee => $"Fee={Number(flatFee.Fee)}",
            DoorSplitDealDto doorSplit => $"ArtistDoorPercent={Number(doorSplit.ArtistDoorPercent)}",
            VersusDealDto versus =>
                $"Guarantee={Number(versus.Guarantee)};ArtistDoorPercent={Number(versus.ArtistDoorPercent)}",
            VenueHireDealDto venueHire => $"HireFee={Number(venueHire.HireFee)}",
            _ => throw new ArgumentOutOfRangeException(nameof(deal), deal, null)
        };
        var payload = Invariant(
            $"{deal.DealType}|{deal.PaymentMethod}|{numbers}|{Instant(period.Start)}|{Instant(period.End)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string Number(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);

    private static string Instant(DateTime value) =>
        DateTime.SpecifyKind(value, DateTimeKind.Utc).ToString("O", CultureInfo.InvariantCulture);
}
