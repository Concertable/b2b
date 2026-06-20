using Concertable.Kernel.Identity;
using Microsoft.AspNetCore.Http;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class TenantContext : ITenantContext, ITenantResolver
{
    private readonly ICurrentUser currentUser;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ITenantRepository repository;

    private Guid? tenantId;
    private bool resolved;

    public TenantContext(
        ICurrentUser currentUser,
        IHttpContextAccessor httpContextAccessor,
        ITenantRepository repository)
    {
        this.currentUser = currentUser;
        this.httpContextAccessor = httpContextAccessor;
        this.repository = repository;
    }

    public Guid? TenantId => tenantId;

    /// <summary>
    /// No HTTP request in scope (worker, outbox dispatcher, event/projection handler) = system caller = filter bypass.
    /// An anonymous HTTP request keeps this <see langword="false"/>, so it fails closed (sees nothing) instead of open.
    /// </summary>
    public bool IsHost => httpContextAccessor.HttpContext is null;

    public async Task ResolveAsync(CancellationToken ct = default)
    {
        if (resolved || IsHost)
            return;

        resolved = true;

        if (currentUser.Id is not { } userId)
            return;

        var membership = await repository.GetMembershipByUserIdAsync(userId, ct);
        tenantId = membership?.TenantId;
    }
}
