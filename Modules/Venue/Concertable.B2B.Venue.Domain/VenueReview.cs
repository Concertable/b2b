namespace Concertable.B2B.Venue.Domain;

public class VenueReview
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Email { get; set; } = null!;
    public double Stars { get; set; }
    public string? Details { get; set; }
}
