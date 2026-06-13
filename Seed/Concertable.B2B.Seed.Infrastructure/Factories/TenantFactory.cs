using Concertable.B2B.Tenant.Domain;
using Concertable.Seed.Identity;

namespace Concertable.B2B.Seed.Infrastructure.Factories;

public static class TenantFactory
{
    // Pass the seed id into Create (not .WithId after) so the raised TenantCreatedDomainEvent carries it.
    public static TenantEntity Create(Guid userId, string email, DateTime createdAt)
        => TenantEntity.Create(email, userId, createdAt, TenantSeedIds.For(userId));
}
