using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

/// <summary>
/// Tenant's implementation of the Authorization module's membership port. Reads the membership row itself, so
/// a role change or removal takes effect on the next request, and carries the revision the resolved request
/// re-checks against.
/// </summary>
internal sealed class MembershipFacts : IMembershipFacts
{
    private readonly TenantDbContext context;

    public MembershipFacts(TenantDbContext context)
    {
        this.context = context;
    }

    public Task<MembershipFact?> GetAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default) =>
        context.Memberships
            .Where(m => m.UserId == userId && m.TenantId == tenantId)
            .Select(m => new MembershipFact(m.TenantId, m.UserId, m.Role, m.AuthorizationVersion))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<MembershipFact>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await context.Memberships
            .Where(m => m.UserId == userId)
            .Select(m => new MembershipFact(m.TenantId, m.UserId, m.Role, m.AuthorizationVersion))
            .ToListAsync(cancellationToken);
}
