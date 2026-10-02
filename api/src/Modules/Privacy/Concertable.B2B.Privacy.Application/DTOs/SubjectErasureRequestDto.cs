namespace Concertable.B2B.Privacy.Application.DTOs;

internal sealed record SubjectErasureRequestDto
{
    public required Guid Id { get; init; }
    public required Guid SubjectId { get; init; }
    public required ErasureState State { get; init; }
    public DateTime RequestedAtUtc { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public string? DeferralReason { get; init; }
}
