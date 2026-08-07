using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Workflows;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class ConcertWorkflowCapabilityRegistryTests
{
    private readonly IConcertWorkflowCapabilityRegistry registry;

    public ConcertWorkflowCapabilityRegistryTests()
    {
        this.registry = new ConcertWorkflowCapabilityRegistry(new Dictionary<DealType, Type>
        {
            [DealType.FlatFee] = typeof(FlatFeeWorkflow),
            [DealType.DoorSplit] = typeof(DoorSplitWorkflow),
            [DealType.Versus] = typeof(VersusWorkflow),
            [DealType.VenueHire] = typeof(VenueHireWorkflow),
        });
    }

    [Fact]
    public void DealTypesWith_AcceptsCheckout_ReturnsCheckoutCapableDealTypesOnly()
    {
        var dealTypes = registry.DealTypesWith<IAcceptsCheckout>();

        Assert.Equal(
            new[] { DealType.FlatFee, DealType.DoorSplit, DealType.Versus }.OrderBy(d => d),
            dealTypes.OrderBy(d => d));
    }
}
