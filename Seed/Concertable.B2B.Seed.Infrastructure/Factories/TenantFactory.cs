using Concertable.B2B.Tenant.Domain;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class TenantFactory
{
    // Pass the seed id into Create (not .WithId after) so the raised TenantCreatedDomainEvent carries it.
    public static TenantEntity Create(Guid userId, string legalName, DateTime createdAt)
        => TenantEntity.Create(legalName, userId, createdAt, TenantSeedIds.For(userId));
}
