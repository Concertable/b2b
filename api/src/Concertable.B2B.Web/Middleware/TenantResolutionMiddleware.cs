using Concertable.B2B.Authorization.Contracts;
using Concertable.Kernel.Identity;

namespace Concertable.B2B.Web.Middleware;

/// <summary>
/// Resolves the current request's tenant once, between authentication and authorization, so it is the single
/// resolution point: EF query filters and the <c>PermissionAuthorizationHandler</c> both read a populated
/// <see cref="ITenantContext"/> (the handler's own memoized <c>ResolveAsync</c> then no-ops). The lookup is
/// memoized and a no-op for anonymous callers, so an unauthenticated request (static files included) pays
/// nothing.
/// <para>
/// It also establishes the interactive execution stance for the request. This host also runs trusted
/// background work, whose stance a request would otherwise inherit and be served unfiltered.
/// </para>
/// </summary>
internal sealed class TenantResolutionMiddleware : IMiddleware
{
    private readonly ITenantResolver tenantResolver;
    private readonly IExecutionScopeActivator executionScopes;

    public TenantResolutionMiddleware(ITenantResolver tenantResolver, IExecutionScopeActivator executionScopes)
    {
        this.tenantResolver = tenantResolver;
        this.executionScopes = executionScopes;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        using var interactive = executionScopes.EnterInteractive();
        await tenantResolver.ResolveAsync(context.RequestAborted);
        await next(context);
    }
}
