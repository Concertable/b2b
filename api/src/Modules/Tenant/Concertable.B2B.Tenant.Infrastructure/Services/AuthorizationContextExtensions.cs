namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal static class AuthorizationContextExtensions
{
    extension(IAuthorizationContext authorization)
    {
        public MembershipSnapshot? FindActor(Guid tenantId) =>
            authorization.Membership is { } actor && actor.TenantId == tenantId ? actor : null;
    }
}
