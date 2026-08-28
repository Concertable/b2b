using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Executors;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class CancelExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly CancelExecutor executor;

    public CancelExecutorTests()
    {
        var behavior = new ImmediateBehavior();
        this.executor = new CancelExecutor(
            this.concertRepository.Object,
            Mock.Of<IDealTypeStrategyFactory<ICancelStep>>(),
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
            .Setup(repository => repository.GetByIdAsync(It.IsAny<int>(), cancellationToken))
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
