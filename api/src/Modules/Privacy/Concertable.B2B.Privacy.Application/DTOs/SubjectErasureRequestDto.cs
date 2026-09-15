namespace Concertable.B2B.Privacy.Application.DTOs;

/// <summary>The state of a subject-erasure request as reported to the admin operator: whether it completed,
/// or deferred (with the reason) pending a live financial obligation.</summary>
public sealed record SubjectErasureRequestDto
{
    public required Guid Id { get; init; }
    public required Guid SubjectId { get; init; }
    public required ErasureState State { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? DeferralReason { get; init; }
}
