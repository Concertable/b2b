using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class InvitationRepository : Repository<TenantInvitationEntity>, IInvitationRepository
{
    private readonly TenantDbContext context;
    private readonly CommandTransactionAccessor transactions;

    public InvitationRepository(
        TenantDbContext context,
        CommandTransactionAccessor transactions) : base(context)
    {
        this.context = context;
        this.transactions = transactions;
    }

    public async Task<TenantInvitationEntity?> GetByIdForUpdateAsync(
        Guid invitationId,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Invitation updates require an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant."Invitations"
             WHERE "Id" = {invitationId}
             FOR UPDATE
             """,
            ct);
        return await context.Invitations.SingleOrDefaultAsync(invitation => invitation.Id == invitationId, ct);
    }

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListInvitationsByTenantAsync(Guid tenantId, CancellationToken ct = default) =>
        await context.Invitations.Where(i => i.TenantId == tenantId).ToListAsync(ct);

    public async Task<IReadOnlyList<TenantInvitationEntity>> ListPendingInvitationsByTenantAsync(Guid tenantId, DateTime now, CancellationToken ct = default) =>
        await context.Invitations
            .Where(i => i.TenantId == tenantId && i.Status == InvitationStatus.Pending && i.ExpiresAt > now)
            .ToListAsync(ct);

    public Task<TenantInvitationEntity?> GetPendingInvitationByEmailAsync(Guid tenantId, string email, CancellationToken ct = default) =>
        context.Invitations.FirstOrDefaultAsync(i => i.TenantId == tenantId && i.Email == email && i.Status == InvitationStatus.Pending, ct);
}
