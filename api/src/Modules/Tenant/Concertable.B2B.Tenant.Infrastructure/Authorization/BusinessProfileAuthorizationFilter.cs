using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

/// <summary>
/// Enforces <see cref="RequiresBusinessProfileAttribute"/> against the active tenant's activated profiles.
/// Registered globally and inert on an endpoint carrying no such metadata, so profile eligibility stays a
/// per-endpoint declaration rather than a second authority axis every request pays for. Runs after
/// authentication and permission authorization: a caller who fails those never reaches it.
/// </summary>
internal sealed class BusinessProfileAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ITenantResolver resolver;
    private readonly ITenantContext tenantContext;
    private readonly ITenantService tenants;

    public BusinessProfileAuthorizationFilter(
        ITenantResolver resolver,
        ITenantContext tenantContext,
        ITenantService tenants)
    {
        this.resolver = resolver;
        this.tenantContext = tenantContext;
        this.tenants = tenants;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<RequiresBusinessProfileAttribute>() is not { } required)
            return;

        await resolver.ResolveAsync(context.HttpContext.RequestAborted);

        if (tenantContext.TenantId is not { } tenantId
            || !await tenants.HasBusinessProfileAsync(tenantId, required.Kind, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
        }
    }
}
