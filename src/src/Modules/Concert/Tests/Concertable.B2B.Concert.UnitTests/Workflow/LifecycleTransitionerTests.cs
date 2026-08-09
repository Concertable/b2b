using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Reunion.Errors;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class LifecycleTransitionerTests
{
    private const int ApplicationId = 42;

    private readonly Mock<IApplicationRepository> repository = new();
    private readonly Mock<IConcertStateMachineRegistry> registry = new();
    private readonly LifecycleTransitioner transitioner;

    public LifecycleTransitionerTests()
    {
        this.transitioner = new LifecycleTransitioner(this.repository.Object, this.registry.Object);
    }

    [Fact]
    public async Task TransitionAsync_MissingApplication_ReturnsNotFound()
    {
        this.repository
            .Setup(repository => repository.GetByIdAsync(ApplicationId, CancellationToken.None))
            .ReturnsAsync((ApplicationEntity?)null);

        var result = await this.transitioner.TransitionAsync(ApplicationId, Trigger.Reject);

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(ErrorKind.NotFound, error.Definition.Kind);
        this.repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionAsync_InvalidTransition_ReturnsConflictWithoutRunningEffect()
    {
        var application = StandardApplication.Create(1, 2, DealType.FlatFee, Guid.NewGuid(), Guid.NewGuid());
        var effectRan = false;
        this.repository
            .Setup(repository => repository.GetByIdAsync(ApplicationId, CancellationToken.None))
            .ReturnsAsync(application);
        this.registry
            .Setup(registry => registry.Get(DealType.FlatFee))
            .Returns(new LifecycleStateMachine([]));

        var result = await this.transitioner.TransitionAsync(
            ApplicationId,
            Trigger.Reject,
            _ =>
            {
                effectRan = true;
                return Task.CompletedTask;
            });

        Assert.True(result.TryGetError(out var error));
        Assert.Equal(ErrorKind.Conflict, error.Definition.Kind);
        Assert.False(effectRan);
        this.repository.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TransitionAsync_ValidTransition_RunsEffectAndSaves()
    {
        var application = StandardApplication.Create(1, 2, DealType.FlatFee, Guid.NewGuid(), Guid.NewGuid());
        var effectRan = false;
        this.repository
            .Setup(repository => repository.GetByIdAsync(ApplicationId, CancellationToken.None))
            .ReturnsAsync(application);
        this.registry
            .Setup(registry => registry.Get(DealType.FlatFee))
            .Returns(new LifecycleStateMachine(new Dictionary<(LifecycleState, Trigger), LifecycleState>
            {
                [(LifecycleState.Applied, Trigger.Reject)] = LifecycleState.Rejected
            }));

        var result = await this.transitioner.TransitionAsync(
            ApplicationId,
            Trigger.Reject,
            _ =>
            {
                effectRan = true;
                return Task.CompletedTask;
            });

        Assert.True(result.TryGetValue(out var transitioned));
        Assert.Same(application, transitioned);
        Assert.Equal(LifecycleState.Rejected, application.State);
        Assert.True(effectRan);
        this.repository.Verify(repository => repository.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
