using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;
using Moq;
using Reunion;

namespace Concertable.B2B.Concert.UnitTests.Services.Completion;

public sealed class CompletionRunnerTests
{
    private readonly Mock<IConcertRepository> repository = new();
    private readonly Mock<ICompleteExecutor> executor = new();
    private readonly Mock<IScoped<ICompleteExecutor>> completion = new();
    private readonly CompletionRunner sut;

    public CompletionRunnerTests()
    {
        sut = new CompletionRunner(repository.Object, completion.Object, Mock.Of<ILogger<CompletionRunner>>());
        executor.Setup(e => e.CompleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SettlementOutcome, FinishConcertError>(SettlementOutcome.Settled));
        completion.Setup(s => s.RunAsync(It.IsAny<Func<ICompleteExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>()))
            .Returns<Func<ICompleteExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>(action => action(executor.Object));
    }

    [Fact]
    public async Task RunAsync_CompletesEveryEndedConcert()
    {
        repository.Setup(r => r.GetEndedPendingCompletionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);

        await sut.RunAsync();

        executor.Verify(e => e.CompleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ContinuesWhenCompletionIsRefused()
    {
        repository.Setup(r => r.GetEndedPendingCompletionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        executor.Setup(e => e.CompleteAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Failure<SettlementOutcome, FinishConcertError>(new FinishConcertError.ConcertNotEnded()));

        await sut.RunAsync();

        executor.Verify(e => e.CompleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(3, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_PropagatesInfrastructureFailure()
    {
        repository.Setup(r => r.GetEndedPendingCompletionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([1, 2, 3]);
        executor.Setup(e => e.CompleteAsync(2, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync());

        executor.Verify(e => e.CompleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(2, It.IsAny<CancellationToken>()), Times.Once);
        executor.Verify(e => e.CompleteAsync(3, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_DoesNothingWhenNoConcertHasEnded()
    {
        repository.Setup(r => r.GetEndedPendingCompletionIdsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await sut.RunAsync();

        executor.Verify(e => e.CompleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}