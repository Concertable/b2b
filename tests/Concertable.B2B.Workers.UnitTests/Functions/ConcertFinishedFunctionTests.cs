using Concertable.B2B.Concert.Application.Executors;
using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Models;
using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using Concertable.DataAccess.Application;
using Reunion;
using Microsoft.Extensions.Logging;
using Moq;
using Concertable.B2B.Workers.Functions;
using Xunit;

namespace Concertable.B2B.Workers.UnitTests.Functions;

public sealed class ConcertCompletionRunnerTests
{
    private readonly Mock<IConcertRepository> concertRepository;
    private readonly Mock<ICompleteExecutor> completeExecutor;
    private readonly Mock<IScoped<ICompleteExecutor>> completion;
    private readonly Mock<ILogger<ConcertCompletionRunner>> logger;
    private readonly ConcertCompletionRunner sut;

    public ConcertCompletionRunnerTests()
    {
        this.concertRepository = new Mock<IConcertRepository>();
        this.completeExecutor = new Mock<ICompleteExecutor>();
        this.completion = new Mock<IScoped<ICompleteExecutor>>();
        this.logger = new Mock<ILogger<ConcertCompletionRunner>>();
        this.sut = new ConcertCompletionRunner(
            this.concertRepository.Object,
            this.completion.Object,
            this.logger.Object);

        this.completeExecutor
            .Setup(p => p.CompleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<SettlementOutcome, FinishConcertError>(SettlementOutcome.Settled));
        this.completion
            .Setup(s => s.RunAsync(It.IsAny<Func<ICompleteExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>()))
            .Returns<Func<ICompleteExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>(
                action => action(this.completeExecutor.Object));
    }

    [Fact]
    public async Task RunAsync_EndedConcerts_CompletesEachConcert()
    {
        this.concertRepository.Setup(r => r.GetEndedPendingCompletionIdsAsync(default)).ReturnsAsync([1, 2, 3]);

        await this.sut.RunAsync();

        this.completeExecutor.Verify(p => p.CompleteAsync(1, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(2, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task RunAsync_OneCompletionRefused_ContinuesProcessing()
    {
        this.concertRepository.Setup(r => r.GetEndedPendingCompletionIdsAsync(default)).ReturnsAsync([1, 2, 3]);
        this.completeExecutor.Setup(p => p.CompleteAsync(2, default)).ReturnsAsync(
            Result.Failure<SettlementOutcome, FinishConcertError>(new FinishConcertError.ConcertNotEnded()));

        await this.sut.RunAsync();

        this.completeExecutor.Verify(p => p.CompleteAsync(1, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(2, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(3, default), Times.Once);
    }

    [Fact]
    public async Task RunAsync_CompleteThrows_PropagatesInfrastructureFailure()
    {
        this.concertRepository.Setup(r => r.GetEndedPendingCompletionIdsAsync(default)).ReturnsAsync([1, 2, 3]);
        this.completeExecutor.Setup(p => p.CompleteAsync(2, default)).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => this.sut.RunAsync());

        this.completeExecutor.Verify(p => p.CompleteAsync(1, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(2, default), Times.Once);
        this.completeExecutor.Verify(p => p.CompleteAsync(3, default), Times.Never);
    }

    [Fact]
    public async Task RunAsync_NoEndedConcerts_DoesNotCompleteAnyConcert()
    {
        this.concertRepository.Setup(r => r.GetEndedPendingCompletionIdsAsync(default)).ReturnsAsync([]);

        await this.sut.RunAsync();

        this.completeExecutor.Verify(
            p => p.CompleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}

public sealed class ConcertFinishedFunctionTests
{
    [Fact]
    public async Task Run_Always_DelegatesToRunner()
    {
        var runner = new Mock<IConcertCompletionRunner>();
        var sut = new ConcertFinishedFunction(runner.Object);

        await sut.Run(null!);

        runner.Verify(r => r.RunAsync(default), Times.Once);
    }
}
