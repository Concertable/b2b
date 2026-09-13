namespace Concertable.B2B.Tenant.Application.Tax;

/// <summary>The registration policy: whether VAT applies to a supply and, if so, its decomposition. Generic (holds
/// the region calculator) — the single place the "supplier VAT-registered ⇒ decompose, else none" rule lives.</summary>
internal interface IVatPolicy
{
    /// <summary>Decompose a VAT-inclusive <paramref name="gross"/> for a supplier with <paramref name="supplierVatNumber"/>
    /// (null/blank ⇒ not registered ⇒ <see cref="VatCalculation.None"/>).</summary>
    VatCalculation Apply(decimal gross, string? supplierVatNumber);
}
