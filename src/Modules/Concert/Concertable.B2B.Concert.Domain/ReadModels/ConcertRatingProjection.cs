namespace Concertable.B2B.Concert.Domain.ReadModels;

public class ConcertRatingProjection
{
    public int ConcertId { get; set; }
    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
