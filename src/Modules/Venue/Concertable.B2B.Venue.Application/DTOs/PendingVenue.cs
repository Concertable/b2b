namespace Concertable.B2B.Venue.Application.DTOs;

/// <summary>A row of the platform venue-approval queue.</summary>
internal sealed record PendingVenue
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string Avatar { get; init; }
    public required string County { get; init; }
    public required string Town { get; init; }
}
