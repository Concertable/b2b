using Concertable.B2B.DataAccess.Infrastructure;
using Reunion;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class ResultOutcomeTests
{
    [Fact]
    public void IsFailure_ReunionFailures_ReturnsTrue()
    {
        Assert.True(ResultOutcome.IsFailure(Result.Failure("failure")));
        Assert.True(ResultOutcome.IsFailure(Result.Failure<int>("failure")));
        Assert.True(ResultOutcome.IsFailure(Result.Failure<int, string>("failure")));
        Assert.True(ResultOutcome.IsFailure(UnitResult.Failure("failure")));
    }

    [Fact]
    public void IsFailure_ReunionSuccesses_ReturnsFalse()
    {
        Assert.False(ResultOutcome.IsFailure(Result.Success()));
        Assert.False(ResultOutcome.IsFailure(Result.Success(1)));
        Assert.False(ResultOutcome.IsFailure(Result.Success<int, string>(1)));
        Assert.False(ResultOutcome.IsFailure(UnitResult.Success<string>()));
    }

    [Fact]
    public void IsFailure_UnrelatedResult_ReturnsFalse()
    {
        Assert.False(ResultOutcome.IsFailure(new UnrelatedResult(true)));
    }

    private sealed record UnrelatedResult(bool IsFailure);
}
