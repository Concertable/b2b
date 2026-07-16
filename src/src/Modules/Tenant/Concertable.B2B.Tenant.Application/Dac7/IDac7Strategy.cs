namespace Concertable.B2B.Tenant.Application.Dac7;

/// <summary>
/// The per-<see cref="Jurisdiction"/> DAC7 rule: what makes a seller's tax identity complete &amp; valid for
/// their region. A keyed strategy resolver (see <c>api/docs/CODE_PATTERNS.md</c>) — <see cref="Dac7Strategy"/>
/// is the facade that maps jurisdiction → concrete strategy, each concrete strategy implements this same
/// interface. Consumers (the org-form validator; the Phase-2 payout gate; the Phase-3 nag) inject this and
/// pass the tenant's jurisdiction; they never branch on region. An unmapped jurisdiction throws.
/// </summary>
internal interface IDac7Strategy
{
    /// <summary>
    /// Whether <paramref name="compliance"/> is complete &amp; valid to report a seller in
    /// <paramref name="jurisdiction"/> — null (never captured) is not complete. The single source of truth
    /// the payout gate and the dashboard nag both consume.
    /// </summary>
    bool IsComplete(Jurisdiction jurisdiction, Compliance? compliance);

    /// <summary>Whether <paramref name="vatNumber"/> is a well-formed VAT number for <paramref name="jurisdiction"/>.</summary>
    bool IsValidVatNumber(Jurisdiction jurisdiction, string vatNumber);

    /// <summary>The user-facing message describing a valid VAT number for <paramref name="jurisdiction"/> — shown when <see cref="IsValidVatNumber"/> fails, so the wording stays out of jurisdiction-agnostic callers.</summary>
    string DescribeVatNumberRequirement(Jurisdiction jurisdiction);

    /// <summary>The jurisdiction's org-form field labels (Tier-1 reference data) — resolved here so the org read never reads a region's options directly.</summary>
    Dac7FieldLabels GetFieldLabels(Jurisdiction jurisdiction);
}
