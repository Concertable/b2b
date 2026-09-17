namespace Concertable.B2B.Tenant.Infrastructure.Mappers;

internal static class QueryableMembershipMappers
{
    extension(IQueryable<TenantMembershipEntity> memberships)
    {
        // Filter on the membership entity's own columns before projecting — a predicate over the
        // projected record doesn't translate, so any Where must sit on TenantMembershipEntity.
        public IQueryable<UserMembership> ToUserMemberships(
            IQueryable<TenantEntity> tenants,
            IQueryable<TenantBusinessProfileEntity> businessProfiles) =>
            memberships.Join(
                tenants,
                m => m.TenantId,
                t => t.Id,
                (m, t) => new UserMembership(
                    m.Id,
                    m.TenantId,
                    t.LegalName,
                    m.Role,
                    m.PermissionVersion,
                    businessProfiles
                        .Where(p => p.TenantId == m.TenantId && p.RetiredAt == null)
                        .Select(p => p.Kind)
                        .ToList()));
    }
}
