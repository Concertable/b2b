using Concertable.B2B.Authorization.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Web.Middleware;

internal sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly ITenantResolver tenantResolver;

    public TenantResolutionMiddleware(ITenantResolver tenantResolver)
    {
        this.tenantResolver = tenantResolver;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        await tenantResolver.ResolveAsync(context.RequestAborted);
        await next(context);
    }
}
