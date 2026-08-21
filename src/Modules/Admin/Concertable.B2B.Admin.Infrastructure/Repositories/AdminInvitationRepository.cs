using Concertable.B2B.Admin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Repositories;

internal sealed class AdminInvitationRepository : Repository<AdminInvitationEntity>, IAdminInvitationRepository
{
    private readonly AdminDbContext context;

    public AdminInvitationRepository(AdminDbContext context) : base(context)
    {
        this.context = context;
    }

    public async Task<IReadOnlyList<AdminInvitationEntity>> ListPendingInvitationsAsync(DateTime now, CancellationToken ct = default) =>
        await context.AdminInvitations
            .Where(i => i.Status == AdminInvitationStatus.Pending && i.ExpiresAt > now)
            .ToListAsync(ct);

    public Task<AdminInvitationEntity?> GetPendingInvitationByEmailAsync(string email, CancellationToken ct = default) =>
        context.AdminInvitations.FirstOrDefaultAsync(i => i.Email == email && i.Status == AdminInvitationStatus.Pending, ct);
}
