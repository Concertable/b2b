namespace Concertable.Artist.Domain;

public class ArtistRatingProjection
{
    public int ArtistId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
