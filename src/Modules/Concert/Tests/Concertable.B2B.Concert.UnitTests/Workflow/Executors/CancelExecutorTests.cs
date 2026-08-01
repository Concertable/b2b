using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow.Executors;

public sealed class CancelExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly CancelExecutor executor;

    public CancelExecutorTests()
    {
        this.executor = new CancelExecutor(
            Mock.Of<ILifecycleTransitioner>(),
            Mock.Of<IConcertWorkflowFactory>(),
            Mock.Of<IDealResolver>(),
            this.concertRepository.Object,
            Mock.Of<ILogger<CancelExecutor>>());
    }

    [Fact]
    public async Task CancelAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.concertRepository
            .Setup(r => r.GetByIdWithBookingAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.executor.CancelAsync(42, cancellationToken));
    }
}
