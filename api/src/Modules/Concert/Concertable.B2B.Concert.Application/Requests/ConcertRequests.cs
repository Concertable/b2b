namespace Concertable.B2B.Concert.Application.Requests;

/// <summary>
/// The scope is not a parameter: a summary share discloses the summary and nothing else. Widening it is a
/// consent design, not another value on this request.
/// </summary>
internal sealed record ShareConcertSummaryRequest
{
    public Guid RequestId { get; init; }
    public Guid RecipientTenantId { get; init; }
    public Guid? RecipientMembershipId { get; init; }
    public long ExpectedAccessVersion { get; init; }
    public DateTime? ValidUntil { get; init; }
}

internal sealed record AssignConcertMemberRequest
{
    public Guid MembershipId { get; init; }
    public long ExpectedAccessVersion { get; init; }
}

internal sealed record UpdateConcertRequest
{
    public required string Name { get; init; }
    public required string About { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
}

internal sealed record DoorRevenueRequest
{
    /// <summary>External take only (excludes Concertable's own ticket sales, which settlement already knows).</summary>
    public decimal DoorRevenue { get; init; }
}
