using Concertable.B2B.Concert.Contracts.Enums;

namespace Concertable.B2B.Concert.Application.Responses;

internal sealed record ConcertShareResponse
{
    public Guid GrantId { get; init; }
    public Guid ToTenantId { get; init; }
    public Guid? ToMemberUserId { get; init; }
    public ConcertAccessScope Scope { get; init; }
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
