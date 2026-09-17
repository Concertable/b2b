using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Booking.Contracts;

/// <summary>A signed booking contract in the subject's portable export (GDPR arts. 15/20). RETAINED — read-only,
/// never mutated by erasure; it survives for the contract-limitation window.</summary>
public sealed record SubjectContractDto
{
    public required string VenueName { get; init; }
    public required string ArtistName { get; init; }
    public required DealType DealType { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
