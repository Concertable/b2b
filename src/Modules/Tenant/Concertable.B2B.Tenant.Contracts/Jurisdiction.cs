namespace Concertable.B2B.Tenant.Contracts;

/// <summary>
/// A tenant's tax jurisdiction, fixed at provisioning. The closed key that drives every region-varying DAC7
/// rule: the <c>IDac7Strategy</c> that decides what makes a seller's tax identity complete &amp; valid, and the
/// per-region reference data (VAT format, seller-id label, reporting authority). A new region is added here,
/// given its strategy + options, and every consumer picks it up without branching — an unmapped jurisdiction
/// throws rather than silently falling back to another region's rules.
/// </summary>
public enum Jurisdiction
{
    Gb = 1,
}
