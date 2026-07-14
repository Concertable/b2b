using System.Security.Cryptography;
using System.Text;
using Concertable.B2B.Concert.Application.Interfaces;
using static System.FormattableString;

namespace Concertable.B2B.Concert.Application.Renderers;

internal sealed class TermsFingerprintCalculator : ITermsFingerprintCalculator
{
    private readonly IDealTermsSerializer termsSerializer;

    public TermsFingerprintCalculator(IDealTermsSerializer termsSerializer) => this.termsSerializer = termsSerializer;

    public string Calculate(IDeal deal, DateRange period)
    {
        var numbers = termsSerializer.Serialize(deal);
        var payload = Invariant(
            $"{deal.DealType}|{deal.PaymentMethod}|{numbers}|{TermsFingerprintFormat.Instant(period.Start)}|{TermsFingerprintFormat.Instant(period.End)}");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}
