using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class TermsFingerprintCalculator : ITermsFingerprintCalculator
{
    public string Calculate(IContract contract, DateRange period)
    {
        var numbers = contract switch
        {
            FlatFeeContract c => Invariant($"Fee={c.Fee}"),
            VenueHireContract c => Invariant($"HireFee={c.HireFee}"),
            DoorSplitContract c => Invariant($"ArtistDoorPercent={c.ArtistDoorPercent}"),
            VersusContract c => Invariant($"Guarantee={c.Guarantee};ArtistDoorPercent={c.ArtistDoorPercent}"),
            _ => throw new ArgumentOutOfRangeException(nameof(contract), contract.ContractType, "Unknown contract type")
        };
        var payload = Invariant(
            $"{contract.ContractType}|{contract.PaymentMethod}|{numbers}|{period.Start:O}|{period.End:O}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
