namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>UK VAT arithmetic at the standard 20% rate. Decomposes a VAT-inclusive gross: net is the gross divided
/// out at 2dp (away-from-zero), and the returned VAT is the exact remainder, so <c>net + vat == gross</c>.</summary>
internal sealed class UkVatCalculator : IVatCalculator
{
    public decimal Rate => 0.20m;

    public decimal Calculate(decimal gross)
        => gross - Math.Round(gross / (1 + Rate), 2, MidpointRounding.AwayFromZero);
}
