using Concertable.B2B.Tenant.Domain;
using static Concertable.Seed.Identity.Extensions.EntityReflectionExtensions;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class TenantFactory
{
    public static TenantEntity Create(Guid userId, string legalName, DateTime createdAt)
        => TenantEntity.Create(legalName, userId, createdAt).WithId(TenantSeedIds.For(userId));
}
