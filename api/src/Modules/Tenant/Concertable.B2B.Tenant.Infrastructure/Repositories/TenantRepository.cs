using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.DataAccess.Infrastructure.Locking;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    private readonly TenantDbContext context;
    private readonly UnitOfWorkAccessor unitOfWorkAccessor;

    public TenantRepository(
        TenantDbContext context,
        UnitOfWorkAccessor unitOfWorkAccessor) : base(context)
    {
        this.context = context;
        this.unitOfWorkAccessor = unitOfWorkAccessor;
    }

    public async Task<bool> ExistsByIdWithKeyShareLockAsync(Guid tenantId, CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Tenant locking requires an active transaction.");
        await unitOfWork.EnlistAsync(context, ct);
        return await context.Tenants.LockRowsAsync(tenant => tenant.Id == tenantId, RowLock.KeyShare, ct);
    }

    public Task<Sha256Digest?> GetSystemRolesFingerprintAsync(CancellationToken ct = default) =>
        context.SystemRolesFingerprints.AsNoTracking()
            .Where(state => state.Id == 1)
            .Select(state => (Sha256Digest?)state.Value)
            .SingleOrDefaultAsync(ct);

    public async Task<TenantEntity?> GetByIdWithUpdateLockAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var unitOfWork = unitOfWorkAccessor.Current
            ?? throw new InvalidOperationException("Tenant administration requires an active unit of work.");
        await unitOfWork.EnlistAsync(context, ct);
        await context.Tenants.LockRowsAsync(tenant => tenant.Id == tenantId, RowLock.Update, ct);
        return await context.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == tenantId, ct);
    }

    public async Task<TenantEntity?> GetByCreatedByUserIdForCreationAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        if (unitOfWorkAccessor.Current is { } unitOfWork)
            await unitOfWork.EnlistAsync(context, ct);
        else if (context.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Tenant creation requires an active transaction.");
        await context.Database.AcquireAdvisoryLockAsync(AdvisoryLock.TenantCreation, userId, ct);
        return await context.Tenants.SingleOrDefaultAsync(tenant => tenant.CreatedByUserId == userId, ct);
    }

    public Task<TenantBusinessDetails?> GetTenantBusinessDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Tenants
            .Where(t => t.Id == tenantId)
            .ToTenantBusinessDetails(context.BusinessActivities)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TenantBusinessDetails>> GetTenantBusinessDetailsByTenantIdsAsync(
        IReadOnlySet<Guid> tenantIds,
        CancellationToken ct = default) =>
        await context.Tenants
            .Where(t => tenantIds.Contains(t.Id))
            .ToTenantBusinessDetails(context.BusinessActivities)
            .ToListAsync(ct);

    public Task<bool> HasActiveBusinessActivityAsync(
        Guid tenantId,
        TenantBusinessActivityKind kind,
        CancellationToken ct = default) =>
        context.BusinessActivities
            .AnyAsync(activity => activity.TenantId == tenantId && activity.Kind == kind && activity.RetiredAt == null, ct);

    public async Task RemoveBusinessActivitiesByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var activities = await context.BusinessActivities
            .Where(activity => activity.TenantId == tenantId)
            .ToListAsync(ct);

        context.BusinessActivities.RemoveRange(activities);
    }
}
