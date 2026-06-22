using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.ReadModels;

/// <summary>
/// Denormalized read model of an artist, owned by the Concert module.
/// Kept in sync via <c>ArtistChangedEvent</c> projections so the Concert module
/// can join artist data without crossing into the Artist module's DB context.
/// </summary>
public sealed class ArtistReadModel : IIdEntity
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = null!;
    public string Avatar { get; set; } = null!;
    public string BannerUrl { get; set; } = null!;
    public Address Address { get; set; } = null!;
    public string Email { get; set; } = null!;
    public ICollection<ArtistReadModelGenre> Genres { get; set; } = [];
}
