using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Deal.Contracts.Enums;

namespace Concertable.B2B.Application.Infrastructure;

internal sealed class ApplicationModule : IApplicationModule
{
    private readonly IApplicationDashboardService dashboardService;

    public ApplicationModule(IApplicationDashboardService dashboardService) =>
        this.dashboardService = dashboardService;

    public bool RequiresApplyCheckout(DealType dealType) => dealType == DealType.VenueHire;

    public bool RequiresAcceptCheckout(DealType dealType) => dealType != DealType.VenueHire;

    public Task<int> GetVenuePendingCountAsync(
        Guid venueTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetVenuePendingCountAsync(venueTenantId, ct);

    public Task<ArtistApplicationDashboardCounts> GetArtistDashboardCountsAsync(
        Guid artistTenantId,
        CancellationToken ct = default) =>
        dashboardService.GetArtistCountsAsync(
            artistTenantId,
            Enum.GetValues<DealType>().Where(RequiresAcceptCheckout).ToHashSet(),
            ct);

}
