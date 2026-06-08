namespace Concertable.B2B.Artist.Domain;

public sealed class ArtistRatingProjection
{
    public int ArtistId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
