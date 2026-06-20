using Concertable.Kernel.Identity;

namespace Concertable.B2B.Web.Middleware;

/// <summary>
/// Resolves the current request's tenant once, between authentication and authorization, so it is the single
/// resolution point: EF query filters and the <c>PermissionAuthorizationHandler</c> both read a populated
/// <see cref="ITenantContext"/> (the handler's own memoized <c>ResolveAsync</c> then no-ops). The lookup is
/// memoized and a no-op for anonymous/host callers, so an unauthenticated request (static files included) pays
/// nothing.
/// </summary>
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
