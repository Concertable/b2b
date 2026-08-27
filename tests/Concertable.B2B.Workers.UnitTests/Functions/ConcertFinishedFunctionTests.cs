using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Application.Errors;
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
    private readonly Mock<IFinishExecutor> finishExecutor;
    private readonly Mock<IScoped<IFinishExecutor>> completion;
    private readonly Mock<ILogger<ConcertCompletionRunner>> logger;
    private readonly ConcertCompletionRunner sut;

    public ConcertCompletionRunnerTests()
    {
        concertRepository = new Mock<IConcertRepository>();
        finishExecutor = new Mock<IFinishExecutor>();
        completion = new Mock<IScoped<IFinishExecutor>>();
        logger = new Mock<ILogger<ConcertCompletionRunner>>();
        sut = new ConcertCompletionRunner(concertRepository.Object, completion.Object, logger.Object);

        finishExecutor.Setup(p => p.FinishAsync(It.IsAny<int>())).ReturnsAsync(
            Result.Success<SettlementOutcome, FinishConcertError>(SettlementOutcome.Settled));
        completion
            .Setup(s => s.RunAsync(It.IsAny<Func<IFinishExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>()))
            .Returns<Func<IFinishExecutor, Task<Result<SettlementOutcome, FinishConcertError>>>>(
                action => action(finishExecutor.Object));
    }

    [Fact]
    public async Task RunAsync_ShouldCallFinishAsync_ForEachEndedConcert()
    {
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([1, 2, 3]);

        await sut.RunAsync();

        finishExecutor.Verify(p => p.FinishAsync(1), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(2), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(3), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldContinueProcessing_WhenOneFinishFails()
    {
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([1, 2, 3]);
        finishExecutor.Setup(p => p.FinishAsync(2)).ReturnsAsync(
            Result.Failure<SettlementOutcome, FinishConcertError>(new FinishConcertError.ConcertNotEnded()));

        await sut.RunAsync();

        finishExecutor.Verify(p => p.FinishAsync(1), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(2), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(3), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldPropagateInfrastructureFailure()
    {
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([1, 2, 3]);
        finishExecutor.Setup(p => p.FinishAsync(2)).ThrowsAsync(new InvalidOperationException());

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunAsync());

        finishExecutor.Verify(p => p.FinishAsync(1), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(2), Times.Once);
        finishExecutor.Verify(p => p.FinishAsync(3), Times.Never);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCallFinishAsync_WhenNoEndedConcerts()
    {
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([]);

        await sut.RunAsync();

        finishExecutor.Verify(p => p.FinishAsync(It.IsAny<int>()), Times.Never);
    }
}

public sealed class ConcertFinishedFunctionTests
{
    [Fact]
    public async Task Run_ShouldDelegateToRunner()
    {
        var runner = new Mock<IConcertCompletionRunner>();
        var sut = new ConcertFinishedFunction(runner.Object);

        await sut.Run(null!);

        runner.Verify(r => r.RunAsync(default), Times.Once);
    }
}
