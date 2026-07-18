namespace Concertable.B2B.Tenant.Contracts;

/// <summary>The three VAT figures a self-billed invoice records for a supply: the net (ex-VAT) amount, the VAT
/// portion, and the rate applied. Decomposed from a VAT-inclusive gross so that <c>Net + Vat == gross</c> exactly.</summary>
public sealed record VatCalculation(decimal Net, decimal Vat, decimal Rate)
{
    /// <summary>No VAT — the supplier is not VAT-registered, so the whole gross is net and the rate is zero.</summary>
    public static VatCalculation None(decimal gross) => new(gross, 0m, 0m);
}
