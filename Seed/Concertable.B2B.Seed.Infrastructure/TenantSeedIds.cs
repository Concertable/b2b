using System.Security.Cryptography;

namespace Concertable.B2B.Seed.Infrastructure;

/// <summary>
/// Deterministic tenant id for seed data — derives a stable <see cref="Guid"/> from the founding user's id so
/// seeded venues/opportunities/contracts can carry their owner tenant without a lookup. This makes the seed
/// race-free against the registration event (the dev seeder writes the same id the handler would skip over)
/// and is a seed-only bootstrap for the one-tenant-per-user era; production tenants get a random id.
/// </summary>
public static class TenantSeedIds
{
    public static Guid For(Guid foundingUserId) => new(MD5.HashData(foundingUserId.ToByteArray()));
}
