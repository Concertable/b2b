namespace Concertable.B2B.Booking.Contracts;

/// <summary>A signed booking contract in the subject's portable export (GDPR arts. 15/20). RETAINED — read-only,
/// never mutated by erasure; it survives for the contract-limitation window.</summary>
public sealed record ContractExport
{
    public required string VenueName { get; init; }
    public required string ArtistName { get; init; }
    public required string DealType { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
