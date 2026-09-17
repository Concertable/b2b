using Concertable.B2B.Tenant.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class InvitationRepository : Repository<TenantInvitationEntity>, IInvitationRepository
{
    private readonly TenantDbContext context;

    public InvitationRepository(TenantDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Invitations.Where(i => i.TenantId == tenantId).ToListAsync(ct);

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByTenantAsync(Guid tenantId, DateTime now, CancellationToken ct = default) =>
        await context.Invitations
            .Where(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .ToListAsync(ct);

    public Task<TenantInvitationEntity?> GetPendingInvitationByEmailAsync(Guid tenantId, string email, CancellationToken ct = default) =>
        context.Invitations.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Email == email && i.Status == InvitationStatus.Pending, ct);

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByEmailAsync(string email, CancellationToken ct = default) =>
        await context.Invitations
            .Where(i => i.Email == email && i.Status == InvitationStatus.Pending)
            .ToListAsync(ct);
}
