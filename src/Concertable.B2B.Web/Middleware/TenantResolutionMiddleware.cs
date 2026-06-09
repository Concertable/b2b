using Concertable.Kernel.Identity;

namespace Concertable.B2B.Web.Middleware;

/// <summary>
/// Resolves the current request's tenant once, after authentication, so EF query filters read a populated
/// <see cref="ITenantContext"/> synchronously at translation time. Runs after the static-file middleware, so
/// only requests that reach the API pay the (memoized, single) lookup; it is a no-op for anonymous callers.
/// </summary>
internal sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate next;

    public TenantResolutionMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        await tenantResolver.ResolveAsync(context.RequestAborted);
        await next(context);
    }
}
