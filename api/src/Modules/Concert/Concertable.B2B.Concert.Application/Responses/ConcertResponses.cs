namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record ConcertSummaryShare
{
    public Guid GrantId { get; init; }
    public long GrantVersion { get; init; }
    public long AccessVersion { get; init; }
    public Guid RecipientTenantId { get; init; }
    public Guid? RecipientMembershipId { get; init; }
    public DateTime ValidFrom { get; init; }
    public DateTime? ValidUntil { get; init; }
}

internal sealed record ConcertUpdateResponse
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string About { get; init; }
    public decimal Price { get; init; }
    public int TotalTickets { get; init; }
    public int AvailableTickets { get; init; }
}
