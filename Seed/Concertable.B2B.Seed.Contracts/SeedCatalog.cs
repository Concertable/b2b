namespace Concertable.B2B.Seed.Contracts;

/// <summary>
/// Canonical seed data for the B2B-published entities (venues, artists, concerts) — the single source the
/// simulator, B2B's own seeders, and downstream projection-test seeders derive from, so the IDs and field
/// values they produce stay in sync across paths and services. The spec→event mapping
/// (<see cref="SeedSpecMappers"/>) is shared; each consumer maps spec→row itself, so cross-path row identity
/// is enforced by parity tests, not guaranteed by construction. Time-relative concert fields derive from
/// <see cref="Now"/>, captured per process — same shape, but not the same absolute value across separate
/// processes (simulator vs each host).
/// </summary>
public sealed partial class SeedCatalog
{
    public DateTime Now { get; }

    public SeedCatalog(TimeProvider timeProvider)
    {
        this.Now = timeProvider.GetUtcNow().UtcDateTime;
    }
}
