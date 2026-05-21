using NetTopologySuite.Geometries;

namespace Concertable.Concert.Domain;

public class VenueReadModel : IIdEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string About { get; set; } = null!;
    public string? County { get; set; }
    public string? Town { get; set; }
    public Point? Location { get; set; }
}
