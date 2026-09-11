using Concertable.Kernel;

namespace Concertable.B2B.Venue.Domain.ReadModels;

public sealed class VenueReview : IIdEntity
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Email { get; set; } = null!;
    public double Stars { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
