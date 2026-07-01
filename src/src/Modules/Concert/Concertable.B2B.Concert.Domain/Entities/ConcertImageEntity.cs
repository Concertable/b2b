using Concertable.Kernel;

namespace Concertable.B2B.Concert.Domain.Entities;

public sealed class ConcertImageEntity : IIdEntity
{
    private ConcertImageEntity() { }

    public int Id { get; private set; }
    public int ConcertId { get; private set; }
    public int Type { get; private set; }
    public string Url { get; private set; } = null!;
    public ConcertEntity Concert { get; set; } = null!;

    public static ConcertImageEntity Create(int concertId, int type, string url) => new()
    {
        ConcertId = concertId,
        Type = type,
        Url = url
    };
}
