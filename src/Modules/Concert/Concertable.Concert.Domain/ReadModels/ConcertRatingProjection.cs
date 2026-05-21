namespace Concertable.Concert.Domain;

public class ConcertRatingProjection
{
    public int ConcertId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
