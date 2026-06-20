using Concertable.B2B.Tenant.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>
/// Builds each <c>perm:&lt;name&gt;</c> policy on demand — <see cref="AuthorizationPolicyBuilder.RequireAuthenticatedUser"/>
/// (so anonymous → 401, not 403) plus a <see cref="PermissionRequirement"/> — and delegates everything else to a
/// <see cref="DefaultAuthorizationPolicyProvider"/>: the default and fallback policies, and any non-<c>perm:</c>
/// name such as the surviving <c>Admin</c> policy and every bare <c>[Authorize]</c>. Forgetting that delegation
/// is the one footgun — it would break <c>Admin</c> and all <c>[Authorize]</c>. Registered singleton; there is
/// no startup policy loop.
/// </summary>
internal sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!PermissionPolicy.TryParse(policyName, out var permission, out var persona))
            return fallback.GetPolicyAsync(policyName);

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission, persona))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }
}
