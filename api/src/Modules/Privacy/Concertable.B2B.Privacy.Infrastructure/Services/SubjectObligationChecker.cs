namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class SubjectObligationChecker : ISubjectObligationChecker
{
    private readonly ITenantModule tenantModule;
    private readonly IApplicationModule applicationModule;
    private readonly IBookingModule bookingModule;
    private readonly IConcertModule concertModule;

    public SubjectObligationChecker(
        ITenantModule tenantModule,
        IApplicationModule applicationModule,
        IBookingModule bookingModule,
        IConcertModule concertModule)
    {
        this.tenantModule = tenantModule;
        this.applicationModule = applicationModule;
        this.bookingModule = bookingModule;
        this.concertModule = concertModule;
    }

    public async Task<bool> HasLiveObligationsAsync(Guid subjectId, IReadOnlySet<Guid> capturedTenantIds, CancellationToken ct = default)
    {
        var memberships = await this.tenantModule.GetMembershipsAsync(subjectId, ct);
        var tenantIds = memberships.Select(m => m.TenantId).Concat(capturedTenantIds).ToHashSet();
        if (tenantIds.Count == 0)
            return false;

        return await this.applicationModule.HasLiveObligationsByTenantIdsAsync(tenantIds, ct)
            || await this.bookingModule.HasLiveObligationsByTenantIdsAsync(tenantIds, ct)
            || await this.concertModule.HasLiveObligationsByTenantIdsAsync(tenantIds, ct);
    }
}
