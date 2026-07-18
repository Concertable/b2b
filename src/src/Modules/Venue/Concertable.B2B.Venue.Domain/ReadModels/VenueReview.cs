namespace Concertable.B2B.Venue.Domain.ReadModels;

public sealed class VenueReview
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Email { get; set; } = null!;
    public double Stars { get; set; }
    public string? Details { get; set; }
}
