using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Concertable.B2B.Tenant.Infrastructure.Authorization;

internal sealed class BusinessActivityAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly ITenantResolver resolver;
    private readonly ITenantContext tenantContext;
    private readonly ITenantService tenants;

    public BusinessActivityAuthorizationFilter(
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
        if (context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<RequiresBusinessActivityAttribute>() is not { } required)
            return;

        await resolver.ResolveAsync(context.HttpContext.RequestAborted);

        if (tenantContext.TenantId is not { } tenantId
            || !await tenants.HasBusinessActivityAsync(tenantId, required.Kind, context.HttpContext.RequestAborted))
        {
            context.Result = new ForbidResult();
        }
    }
}
