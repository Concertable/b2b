using Concertable.Contracts;

namespace Concertable.Concert.Domain.ReadModels;

public class ArtistReadModelGenre
{
    public int ArtistReadModelId { get; set; }
    public Genre Genre { get; set; }
    public ArtistReadModel Artist { get; set; } = null!;
}
