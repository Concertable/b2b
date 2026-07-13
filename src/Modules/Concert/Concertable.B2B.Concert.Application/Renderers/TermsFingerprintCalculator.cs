using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class TermsFingerprintCalculator : ITermsFingerprintCalculator
{
    private readonly IContractTermsSerializer termsSerializer;

    public TermsFingerprintCalculator(IContractTermsSerializer termsSerializer) => this.termsSerializer = termsSerializer;

    public string Calculate(IContract contract, DateRange period)
    {
        var numbers = termsSerializer.Serialize(contract);
        var payload = Invariant(
            $"{contract.ContractType}|{contract.PaymentMethod}|{numbers}|{TermsFingerprintFormat.Instant(period.Start)}|{TermsFingerprintFormat.Instant(period.End)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
