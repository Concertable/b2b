using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow.Executors;

public sealed class CancelExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly CancelExecutor executor;

    public CancelExecutorTests()
    {
        var behavior = new ImmediateBehavior();
        this.executor = new CancelExecutor(
            Mock.Of<ILifecycleTransitioner>(),
            Mock.Of<IConcertWorkflowFactory>(),
            Mock.Of<IDealResolver>(),
            this.concertRepository.Object,
            behavior,
            behavior);
    }

    [Fact]
    public async Task CancelAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.concertRepository
            .Setup(r => r.GetByIdForLifecycleAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.executor.CancelAsync(42, cancellationToken));
    }

    private sealed class ImmediateBehavior : IUnitOfWorkBehavior, IOutboxUnitOfWorkBehavior
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    }
}
