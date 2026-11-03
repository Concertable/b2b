using System.Diagnostics.CodeAnalysis;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal static class AuthorizationContextExtensions
{
    extension(IAuthorizationContext authorizationContext)
    {
        public bool TryGetMembership(Guid tenantId, [NotNullWhen(true)] out MembershipSnapshot? membership)
        {
            membership = authorizationContext.Membership.TryGetValue(out var acting) && acting.TenantId == tenantId
                ? acting
                : null;
            return membership is not null;
        }
    }
}
