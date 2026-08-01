using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow.Executors;

public sealed class FinishExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly FinishExecutor executor;

    public FinishExecutorTests()
    {
        this.executor = new FinishExecutor(
            Mock.Of<ILifecycleTransitioner>(),
            Mock.Of<IConcertWorkflowFactory>(),
            Mock.Of<IDealResolver>(),
            this.concertRepository.Object,
            Mock.Of<ISettlementPayeeResolver>(),
            Mock.Of<ITicketPayeeResolver>(),
            Mock.Of<IInvoiceIssuer>(),
            Mock.Of<ITenantModule>(),
            TimeProvider.System,
            Mock.Of<ILogger<FinishExecutor>>());
    }

    [Fact]
    public async Task FinishAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.concertRepository
            .Setup(r => r.GetByIdWithBookingAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.executor.FinishAsync(42, cancellationToken));
    }
}
