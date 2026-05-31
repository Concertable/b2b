using Concertable.B2B.Concert.Infrastructure.Services.Completion;
using FluentResults;
using Microsoft.Extensions.Logging;
using Moq;
using Concertable.B2B.Workers.Functions;
using Xunit;

namespace Concertable.B2B.Workers.UnitTests.Functions;

public class ConcertCompletionRunnerTests
{
    private readonly Mock<IConcertRepository> concertRepository;
    private readonly Mock<ICompletionDispatcher> completionDispatcher;
    private readonly Mock<ILogger<ConcertCompletionRunner>> logger;
    private readonly ConcertCompletionRunner sut;

    public ConcertCompletionRunnerTests()
    {
        concertRepository = new Mock<IConcertRepository>();
        completionDispatcher = new Mock<ICompletionDispatcher>();
        logger = new Mock<ILogger<ConcertCompletionRunner>>();
        sut = new ConcertCompletionRunner(concertRepository.Object, completionDispatcher.Object, logger.Object);

        completionDispatcher.Setup(p => p.FinishAsync(It.IsAny<int>())).ReturnsAsync(Result.Ok());
    }

    [Fact]
    public async Task RunAsync_ShouldCallFinishAsync_ForEachEndedConcert()
    {
        // Arrange
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([1, 2, 3]);

        // Act
        await sut.RunAsync();

        // Assert
        completionDispatcher.Verify(p => p.FinishAsync(1), Times.Once);
        completionDispatcher.Verify(p => p.FinishAsync(2), Times.Once);
        completionDispatcher.Verify(p => p.FinishAsync(3), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldContinueProcessing_WhenOneFinishFails()
    {
        // Arrange
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([1, 2, 3]);
        completionDispatcher.Setup(p => p.FinishAsync(2)).ReturnsAsync(Result.Fail("Payment failed"));

        // Act
        await sut.RunAsync();

        // Assert
        completionDispatcher.Verify(p => p.FinishAsync(1), Times.Once);
        completionDispatcher.Verify(p => p.FinishAsync(2), Times.Once);
        completionDispatcher.Verify(p => p.FinishAsync(3), Times.Once);
    }

    [Fact]
    public async Task RunAsync_ShouldNotCallFinishAsync_WhenNoEndedConcerts()
    {
        // Arrange
        concertRepository.Setup(r => r.GetEndedConfirmedIdsAsync()).ReturnsAsync([]);

        // Act
        await sut.RunAsync();

        // Assert
        completionDispatcher.Verify(p => p.FinishAsync(It.IsAny<int>()), Times.Never);
    }
}

public class ConcertFinishedFunctionTests
{
    [Fact]
    public async Task Run_ShouldDelegateToRunner()
    {
        // Arrange
        var runner = new Mock<IConcertCompletionRunner>();
        var sut = new ConcertFinishedFunction(runner.Object);

        // Act
        await sut.Run(null!);

        // Assert
        runner.Verify(r => r.RunAsync(default), Times.Once);
    }
}
