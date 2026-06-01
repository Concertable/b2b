namespace Concertable.B2B.Artist.Domain;

public sealed class ArtistReview
{
    public int Id { get; set; }
    public int ArtistId { get; set; }
    public string Email { get; set; } = null!;
    public double Stars { get; set; }
    public string? Details { get; set; }
}
