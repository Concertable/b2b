using Reunion;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal static class AuthorizationContextExtensions
{
    extension(IAuthorizationContext authorization)
    {
        public Option<MembershipSnapshot> FindActor(Guid tenantId) =>
            authorization.Membership.TryGetValue(out var actor) && actor.TenantId == tenantId ? actor : null;
    }
}
