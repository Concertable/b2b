namespace Concertable.B2B.Concert.Application.Interfaces;

/// <summary>
/// Deterministic fingerprint of the terms a party consented to (contract type + numbers +
/// payment method + event period). Stored at Apply and recomputed at Accept, so an in-place
/// opportunity edit between the two invalidates the artist's consent instead of silently
/// binding them to terms they never saw.
/// </summary>
internal interface ITermsFingerprintCalculator
{
    string Calculate(IContract contract, DateRange period);
}
