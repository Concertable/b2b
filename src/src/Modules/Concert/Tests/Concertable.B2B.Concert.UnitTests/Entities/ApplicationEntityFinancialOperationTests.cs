using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel.ValueObjects;

namespace Concertable.B2B.Concert.UnitTests.Entities;

public sealed class ApplicationEntityFinancialOperationTests
{
    [Fact]
    public void BeginAcceptance_ReusesOperationId()
    {
        var application = CreateApplication();

        var first = application.BeginAcceptance();
        var second = application.BeginAcceptance();

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
        Assert.Equal(first, application.AcceptanceOperationId);
    }

    [Fact]
    public void BeginCancellation_ReusesPendingOperationId()
    {
        var application = CreateApplication();

        var first = application.BeginCancellation();
        var second = application.BeginCancellation();

        Assert.NotEqual(Guid.Empty, first);
        Assert.Equal(first, second);
        Assert.Equal(first, application.CancellationOperationId);
    }

    [Fact]
    public void BeginCancellation_AfterRejectedOperation_CreatesNewOperationId()
    {
        var application = CreateApplication();
        var first = application.BeginCancellation();
        application.Transition(LifecycleState.CancellationFailed);

        var second = application.BeginCancellation();

        Assert.NotEqual(first, second);
        Assert.Equal(second, application.CancellationOperationId);
    }

    private static ApplicationEntity CreateApplication() =>
        StandardApplication.Create(
            1,
            1,
            DealType.FlatFee,
            Guid.NewGuid(),
            Guid.NewGuid());
}
