using Concertable.B2B.DataAccess.Infrastructure;
using Reunion;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class CommandOutcomeTests
{
    [Fact]
    public void IsFailure_ReunionFailures_ReturnsTrue()
    {
        Assert.True(CommandOutcome.IsFailure(Result.Failure("failure")));
        Assert.True(CommandOutcome.IsFailure(Result.Failure<int>("failure")));
        Assert.True(CommandOutcome.IsFailure(Result.Failure<int, string>("failure")));
        Assert.True(CommandOutcome.IsFailure(UnitResult.Failure("failure")));
    }

    [Fact]
    public void IsFailure_ReunionSuccesses_ReturnsFalse()
    {
        Assert.False(CommandOutcome.IsFailure(Result.Success()));
        Assert.False(CommandOutcome.IsFailure(Result.Success(1)));
        Assert.False(CommandOutcome.IsFailure(Result.Success<int, string>(1)));
        Assert.False(CommandOutcome.IsFailure(UnitResult.Success<string>()));
    }

    [Fact]
    public void IsFailure_UnrelatedResult_ReturnsFalse()
    {
        Assert.False(CommandOutcome.IsFailure(new UnrelatedResult(true)));
    }

    private sealed record UnrelatedResult(bool IsFailure);
}
