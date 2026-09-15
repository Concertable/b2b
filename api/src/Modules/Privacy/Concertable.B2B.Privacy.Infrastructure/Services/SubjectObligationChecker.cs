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

    public async Task<bool> HasLiveObligationsAsync(Guid subjectId, CancellationToken ct = default)
    {
        var memberships = await tenantModule.GetMembershipsAsync(subjectId, ct);
        var tenantIds = memberships.Select(m => m.TenantId).Distinct().ToArray();
        if (tenantIds.Length == 0)
            return false;

        return await applicationModule.HasLiveObligationsAsync(tenantIds, ct)
            || await bookingModule.HasLiveObligationsAsync(tenantIds, ct)
            || await concertModule.HasLiveObligationsAsync(tenantIds, ct);
    }
}
