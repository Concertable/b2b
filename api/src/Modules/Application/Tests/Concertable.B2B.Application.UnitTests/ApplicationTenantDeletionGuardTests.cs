using Concertable.B2B.Application.Application.Interfaces;
using Concertable.B2B.Application.Infrastructure.Services;
using Moq;

namespace Concertable.B2B.Application.UnitTests;

public sealed class ApplicationTenantDeletionGuardTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HasLiveObligations_TakesTheTenantApplicationsLockBeforeCheckingLiveApplications(bool live)
    {
        var tenantId = Guid.NewGuid();
        var steps = new List<string>();
        var repository = new Mock<IApplicationPrivilegedRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.AcquireTenantApplicationsExclusiveLockAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("lock"))
            .Returns(Task.CompletedTask);
        repository
            .Setup(value => value.ExistsLiveByTenantIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .Callback(() => steps.Add("check"))
            .ReturnsAsync(live);
        var guard = new ApplicationTenantDeletionGuard(repository.Object);

        var result = await guard.HasLiveObligationsAsync(tenantId);

        Assert.Equal(live, result);
        Assert.Equal(["lock", "check"], steps);
    }
}
