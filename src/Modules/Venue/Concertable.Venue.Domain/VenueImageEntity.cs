using System.ComponentModel.DataAnnotations.Schema;

namespace Concertable.Venue.Domain;

[Table("VenueImages")]
public class VenueImageEntity : IIdEntity
{
    private VenueImageEntity() { }

    public int Id { get; private set; }
    public int VenueId { get; private set; }
    public string Url { get; private set; } = null!;
    public VenueEntity Venue { get; set; } = null!;

    public static VenueImageEntity Create(int venueId, string url) => new()
    {
        VenueId = venueId,
        Url = url
    };
}
