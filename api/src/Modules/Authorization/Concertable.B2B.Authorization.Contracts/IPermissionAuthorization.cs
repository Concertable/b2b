namespace Concertable.B2B.Authorization.Contracts;

public interface IPermissionAuthorization
{
    Task<AuthorizationDecision> CheckAsync(
        TenantPermission permission,
        ResourceAudience? requiredAudience = null,
        CancellationToken ct = default);

    Task<AuthorizationDecision> RequireAsync(
        TenantPermission permission,
        ResourceAudience requiredAudience,
        CancellationToken ct = default);
}
