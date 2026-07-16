namespace Concertable.B2B.Tenant.Application.DTOs;

/// <summary>
/// The jurisdiction-scoped DAC7 facts the org dashboard + form consume: whether the seller's tax identity is
/// complete (drives the "complete your tax details" nag — the same <c>IDac7Strategy.IsComplete</c> rule the
/// payout gate uses) and the jurisdiction's field labels (drive the form). The frontend renders these; it
/// never re-derives completeness or hardcodes a region's labels.
/// </summary>
public sealed record Dac7Dto
{
    public required bool Complete { get; init; }
    public required string SellerIdentifierLabel { get; init; }
    public required string SellerIdentifierHint { get; init; }
    public required string VatLabel { get; init; }
    public required string VatNumberPlaceholder { get; init; }
}
