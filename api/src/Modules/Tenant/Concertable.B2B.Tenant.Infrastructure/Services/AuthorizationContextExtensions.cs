using System.Diagnostics.CodeAnalysis;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal static class AuthorizationContextExtensions
{
    extension(IAuthorizationContext authorization)
    {
        public bool TryGetMembership(Guid tenantId, [NotNullWhen(true)] out MembershipSnapshot? membership)
        {
            membership = authorization.Membership.TryGetValue(out var acting) && acting.TenantId == tenantId
                ? acting
                : null;
            return membership is not null;
        }
    }
}
