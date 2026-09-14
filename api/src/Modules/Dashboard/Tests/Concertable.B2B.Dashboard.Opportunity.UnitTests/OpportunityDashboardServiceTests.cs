using Concertable.B2B.Application.Contracts;
using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Dashboard.Opportunity.Application;
using Concertable.B2B.Dashboard.Opportunity.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Moq;

namespace Concertable.B2B.Dashboard.Opportunity.UnitTests;

public sealed class OpportunityDashboardServiceTests
{
    [Fact]
    public async Task GetCurrentForVenue_MissingVenue_ReturnsTypedProblem()
    {
        var tenantContext = new Mock<ITenantContext>();
        tenantContext.SetupGet(context => context.TenantId).Returns((Guid?)null);
        var service = new OpportunityDashboardService(
            Mock.Of<IApplicationModule>(),
            Mock.Of<IArtistModule>(),
            Mock.Of<IDealModule>(),
            Mock.Of<IOpportunityModule>(),
            tenantContext.Object,
            TimeProvider.System,
            Mock.Of<IVenueModule>());

        var result = await service.GetOpenAsync();

        Assert.True(result.TryGetError(out var error));
        Assert.IsType<OpportunityDashboardError.MissingVenue>(error);
    }
}
