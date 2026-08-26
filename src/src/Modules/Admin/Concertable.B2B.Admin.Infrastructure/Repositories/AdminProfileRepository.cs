using Concertable.B2B.Admin.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Repositories;

internal sealed class AdminProfileRepository : IAdminProfileRepository
{
    private readonly AdminDbContext context;

    public AdminProfileRepository(AdminDbContext context)
    {
        this.context = context;
    }

    public Task<int> CountAdminsAsync(CancellationToken ct = default) =>
        context.AdminProfiles.CountAsync(ct);

    public async Task<IReadOnlyList<Guid>> ListAdminSubsAsync(CancellationToken ct = default) =>
        await context.AdminProfiles.Select(p => p.Sub).ToListAsync(ct);

    public Task<bool> IsAdminAsync(Guid sub, CancellationToken ct = default) =>
        context.AdminProfiles.AnyAsync(p => p.Sub == sub, ct);

    public void GrantAdmin(Guid sub) => context.AdminProfiles.Add(new AdminProfileEntity(sub));

    public void RemoveAdmin(Guid sub) => context.AdminProfiles.Remove(new AdminProfileEntity(sub));
}
