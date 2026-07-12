using System.Collections.Frozen;
using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class TermsFingerprintCalculator : ITermsFingerprintCalculator
{
    private readonly FrozenDictionary<ContractType, IContractFingerprintComponent> components;

    public TermsFingerprintCalculator(
        FlatFeeFingerprintComponent flatFee,
        DoorSplitFingerprintComponent doorSplit,
        VersusFingerprintComponent versus,
        VenueHireFingerprintComponent venueHire)
    {
        components = new Dictionary<ContractType, IContractFingerprintComponent>
        {
            [ContractType.FlatFee] = flatFee,
            [ContractType.DoorSplit] = doorSplit,
            [ContractType.Versus] = versus,
            [ContractType.VenueHire] = venueHire,
        }.ToFrozenDictionary();
    }

    public string Calculate(IContract contract, DateRange period)
    {
        var numbers = components[contract.ContractType].Compose(contract);
        var payload = Invariant(
            $"{contract.ContractType}|{contract.PaymentMethod}|{numbers}|{TermsFingerprintFormat.Instant(period.Start)}|{TermsFingerprintFormat.Instant(period.End)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
