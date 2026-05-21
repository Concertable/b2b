using Concertable.Shared;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Domain;

[PrimaryKey(nameof(ConcertId), nameof(Genre))]
public class ConcertGenreEntity : IEquatable<ConcertGenreEntity>
{
    public int ConcertId { get; set; }
    public Genre Genre { get; set; }
    public ConcertEntity Concert { get; set; } = null!;

    public bool Equals(ConcertGenreEntity? other) =>
        other is not null && ConcertId == other.ConcertId && Genre == other.Genre;

    public override bool Equals(object? obj) => Equals(obj as ConcertGenreEntity);

    public override int GetHashCode() => HashCode.Combine(ConcertId, Genre);
}
