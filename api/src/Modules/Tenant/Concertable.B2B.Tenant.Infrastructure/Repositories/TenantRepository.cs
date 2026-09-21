using Concertable.B2B.Tenant.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Mappers;
using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Repositories;

internal sealed class TenantRepository : Repository<TenantEntity>, ITenantRepository
{
    private const long CreationLockSeed = 638457220;
    private readonly TenantDbContext context;
    private readonly CommandTransactionAccessor transactions;

    public TenantRepository(
        TenantDbContext context,
        CommandTransactionAccessor transactions) : base(context)
    {
        this.context = context;
        this.transactions = transactions;
    }

    public async Task<TenantEntity?> GetByIdForAdministrationAsync(
        Guid tenantId,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant administration requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT 1
             FROM tenant."Tenants"
             WHERE "Id" = {tenantId}
             FOR UPDATE
             """,
            ct);
        return await context.Tenants.SingleOrDefaultAsync(tenant => tenant.Id == tenantId, ct);
    }

    public async Task<TenantEntity?> GetByCreatedByUserIdForCreationAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var transaction = transactions.Current
            ?? throw new InvalidOperationException("Tenant creation requires an active command transaction.");
        await transaction.EnlistAsync(context, ct);
        await context.Database.ExecuteSqlInterpolatedAsync(
            $"""
             SELECT pg_advisory_xact_lock(hashtextextended(CAST({userId} AS text), {CreationLockSeed}))
             """,
            ct);
        return await context.Tenants.SingleOrDefaultAsync(tenant => tenant.CreatedByUserId == userId, ct);
    }

    public Task<TenantBusinessDetails?> GetTenantBusinessDetailsByTenantIdAsync(Guid tenantId, CancellationToken ct = default) =>
        context.Tenants
            .Where(t => t.Id == tenantId)
            .ToTenantBusinessDetails(context.BusinessActivities)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<TenantBusinessDetails>> GetTenantBusinessDetailsByTenantIdsAsync(
        IReadOnlyCollection<Guid> tenantIds,
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
