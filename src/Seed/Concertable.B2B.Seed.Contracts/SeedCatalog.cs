namespace Concertable.B2B.Seed.Contracts;

/// <summary>
/// Canonical seed data for the B2B-published entities (venues, artists, concerts). Both B2B's own
/// seeders and the seeding simulator project from this fixture so downstream projection state is
/// byte-identical regardless of who produced it.
/// </summary>
public sealed partial class SeedCatalog
{
    public DateTime Now { get; }

    public SeedCatalog(TimeProvider timeProvider)
    {
        this.Now = timeProvider.GetUtcNow().UtcDateTime;
    }
}
