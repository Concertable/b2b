namespace Concertable.B2B.Venue.Domain;

public sealed class VenueRatingProjection
{
    public int VenueId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
