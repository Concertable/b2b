namespace Concertable.Venue.Domain;

public class VenueRatingProjection
{
    public int VenueId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
