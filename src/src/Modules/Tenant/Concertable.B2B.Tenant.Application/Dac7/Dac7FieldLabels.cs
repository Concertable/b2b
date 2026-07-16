namespace Concertable.B2B.Tenant.Application.Dac7;

/// <summary>
/// Tier-1 per-<see cref="Jurisdiction"/> field labels the org form renders (see <c>UkDac7Options</c>).
/// Resolved through <see cref="IDac7Strategy.GetFieldLabels"/> so an agnostic caller never reads a region's
/// options class directly — the same "never branch on region" rule the gate and nag follow.
/// </summary>
internal sealed record Dac7FieldLabels(
    string SellerIdentifierLabel,
    string SellerIdentifierHint,
    string VatLabel,
    string VatNumberPlaceholder);
