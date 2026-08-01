using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Infrastructure.Services.Workflow;
using Moq;

namespace Concertable.B2B.Concert.UnitTests.Workflow;

public sealed class VerifyCoordinatorTests
{
    private readonly Mock<IPaymentVerificationRecorder> recorder = new(MockBehavior.Strict);
    private readonly Mock<IBookingAdvancer> bookingAdvancer = new(MockBehavior.Strict);
    private readonly VerifyCoordinator coordinator;

    public VerifyCoordinatorTests()
    {
        this.coordinator = new VerifyCoordinator(
            this.recorder.Object,
            this.bookingAdvancer.Object);
    }

    [Fact]
    public async Task SucceededAsync_OutcomeReceived_RecordsBeforeAdvancing()
    {
        const int applicationId = 42;
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var sequence = new MockSequence();
        this.recorder
            .InSequence(sequence)
            .Setup(r => r.RecordVerifiedAsync(applicationId, cancellationToken))
            .Returns(Task.CompletedTask);
        this.bookingAdvancer
            .InSequence(sequence)
            .Setup(a => a.AdvanceIfReadyAsync(applicationId, cancellationToken))
            .Returns(Task.CompletedTask);

        await this.coordinator.SucceededAsync(applicationId, cancellationToken);

        this.recorder.VerifyAll();
        this.bookingAdvancer.VerifyAll();
    }

    [Fact]
    public async Task FailedAsync_OutcomeReceived_RecordsBeforeAdvancing()
    {
        const int applicationId = 42;
        const string venueManagerId = "manager-id";
        const string failureMessage = "declined";
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var sequence = new MockSequence();
        this.recorder
            .InSequence(sequence)
            .Setup(r => r.RecordFailedAsync(
                applicationId,
                venueManagerId,
                failureMessage,
                cancellationToken))
            .Returns(Task.CompletedTask);
        this.bookingAdvancer
            .InSequence(sequence)
            .Setup(a => a.AdvanceIfReadyAsync(applicationId, cancellationToken))
            .Returns(Task.CompletedTask);

        await this.coordinator.FailedAsync(
            applicationId,
            venueManagerId,
            failureMessage,
            cancellationToken);

        this.recorder.VerifyAll();
        this.bookingAdvancer.VerifyAll();
    }
}
