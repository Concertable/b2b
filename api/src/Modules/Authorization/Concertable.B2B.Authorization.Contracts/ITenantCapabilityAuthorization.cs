namespace Concertable.B2B.Authorization.Contracts;

public interface ITenantCapabilityAuthorization
{
    Task<AuthorizationDecision> CheckAsync(TenantPermission permission, CancellationToken ct = default);

    Task<AuthorizationDecision> RequireAsync(TenantPermission permission, CancellationToken ct = default);
}
