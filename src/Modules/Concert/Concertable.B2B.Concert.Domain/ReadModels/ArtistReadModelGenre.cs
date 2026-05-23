using Concertable.Contracts;

namespace Concertable.B2B.Concert.Domain.ReadModels;

public class ArtistReadModelGenre
{
    public int ArtistReadModelId { get; set; }
    public Genre Genre { get; set; }
    public ArtistReadModel Artist { get; set; } = null!;
}
