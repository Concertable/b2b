namespace Concertable.Venue.Api.Responses;

public record VenueDetailsResponse
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string About { get; set; }
    public required string BannerUrl { get; set; }
    public string? Avatar { get; set; }
    public double Rating { get; set; }
    public required string County { get; set; }
    public required string Town { get; set; }
    public required string Email { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public bool Approved { get; set; }
}
