using Concertable.Kernel;
using NetTopologySuite.Geometries;

namespace Concertable.B2B.Concert.Domain.ReadModels;

/// <summary>
/// Denormalized read model of a venue, owned by the Concert module.
/// Kept in sync via <c>VenueChangedEvent</c> projections so the Concert module
/// can join venue data without crossing into the Venue module's DB context.
/// </summary>
public sealed class VenueReadModel : IIdEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string About { get; set; } = null!;
    public string? County { get; set; }
    public string? Town { get; set; }
    public Point? Location { get; set; }
}
