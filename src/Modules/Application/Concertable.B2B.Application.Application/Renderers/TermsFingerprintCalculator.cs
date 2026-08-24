using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Domain;

namespace Concertable.B2B.Application.Application.Renderers;

internal sealed class TermsFingerprintCalculator : ITermsFingerprintCalculator
{
    public string Calculate(DealDto deal, DateRange period) =>
        ApplicationTermsFingerprint.Calculate(deal, period);
}
