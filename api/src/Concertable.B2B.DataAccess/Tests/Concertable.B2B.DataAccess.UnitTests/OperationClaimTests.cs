using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.DataAccess.UnitTests;

public sealed class OperationClaimTests
{
    private readonly OperationClaim claim;

    public OperationClaimTests()
    {
        this.claim = new OperationClaim();
    }

    [Fact]
    public void OperationId_BeforeAnyClaim_IsNull()
    {
        Assert.Null(this.claim.OperationId);
    }

    [Fact]
    public void Claim_WhenUnclaimed_MintsAndHoldsTheOperation()
    {
        var operationId = this.claim.Claim();

        Assert.NotEqual(Guid.Empty, operationId);
        Assert.Equal(operationId, this.claim.OperationId);
    }

    [Fact]
    public void Claim_WhenAlreadyHeld_ResumesTheSameOperation()
    {
        var first = this.claim.Claim();

        var second = this.claim.Claim();

        Assert.Equal(first, second);
        Assert.Equal(first, this.claim.OperationId);
    }

    [Fact]
    public void Claim_WithCallerSuppliedId_HoldsThatOperation()
    {
        var operationId = Guid.NewGuid();

        var claimed = this.claim.Claim(operationId);

        Assert.Equal(operationId, claimed);
        Assert.Equal(operationId, this.claim.OperationId);
    }

    [Fact]
    public void Claim_WithTheHeldId_IsIdempotent()
    {
        var operationId = Guid.NewGuid();
        this.claim.Claim(operationId);

        var reclaimed = this.claim.Claim(operationId);

        Assert.Equal(operationId, reclaimed);
    }

    [Fact]
    public void Claim_WithARivalId_ThrowsAndKeepsTheHeldOperation()
    {
        var operationId = this.claim.Claim(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => this.claim.Claim(Guid.NewGuid()));
        Assert.Equal(operationId, this.claim.OperationId);
    }

    [Fact]
    public void Claim_WithAnEmptyId_ThrowsAndClaimsNothing()
    {
        Assert.Throws<ArgumentException>(() => this.claim.Claim(Guid.Empty));
        Assert.Null(this.claim.OperationId);
    }

    [Fact]
    public void IsHeldBy_TheHoldingOperation_IsTrue()
    {
        var operationId = this.claim.Claim();

        Assert.True(this.claim.IsHeldBy(operationId));
    }

    [Fact]
    public void IsHeldBy_AnotherOperation_IsFalse()
    {
        this.claim.Claim();

        Assert.False(this.claim.IsHeldBy(Guid.NewGuid()));
    }

    [Fact]
    public void IsHeldBy_AnEmptyId_IsFalseEvenWhenUnclaimed()
    {
        Assert.False(this.claim.IsHeldBy(Guid.Empty));
    }
}
