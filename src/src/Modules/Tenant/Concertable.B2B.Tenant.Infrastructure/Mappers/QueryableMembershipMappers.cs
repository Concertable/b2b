namespace Concertable.B2B.Tenant.Infrastructure.Mappers;

internal static class QueryableMembershipMappers
{
    extension(IQueryable<TenantMembershipEntity> memberships)
    {
        // Filter on the membership entity's own columns before projecting — a predicate over the
        // projected record doesn't translate, so any Where must sit on TenantMembershipEntity.
        public IQueryable<UserMembership> ToUserMemberships(IQueryable<TenantEntity> tenants) =>
            memberships.Join(
                tenants,
                m => m.TenantId,
                t => t.Id,
                (m, t) => new UserMembership(m.TenantId, t.LegalName, t.Type, m.Role));
    }
}
