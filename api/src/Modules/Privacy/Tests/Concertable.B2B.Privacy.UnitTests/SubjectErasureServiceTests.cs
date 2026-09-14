using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Privacy.Domain.Entities;
using Concertable.B2B.Privacy.Domain.Lifecycle;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.User.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class SubjectErasureServiceTests
{
    private readonly Mock<ISubjectErasureRepository> repository = new();
    private readonly Mock<ISubjectObligationChecker> obligationChecker = new();
    private readonly Mock<IUserModule> userModule = new();
    private readonly Mock<ITenantModule> tenantModule = new();
    private readonly Mock<IConversationsModule> conversationsModule = new();
    private readonly SubjectErasureService service;

    public SubjectErasureServiceTests()
    {
        repository.Setup(r => r.InsertAsync(It.IsAny<SubjectErasureRequestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectErasureRequestEntity e, CancellationToken _) => e);
        userModule.Setup(u => u.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserDto { Id = Guid.NewGuid(), Email = "subject@test.invalid" });
        tenantModule.Setup(t => t.SeverMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        this.service = new SubjectErasureService(
            repository.Object,
            obligationChecker.Object,
            new ErasureStateMachine(),
            userModule.Object,
            tenantModule.Object,
            conversationsModule.Object,
            TimeProvider.System,
            NullLogger<SubjectErasureService>.Instance);
    }

    [Fact]
    public async Task RequestErasureAsync_NoObligations_CompletesAndRunsTheFanOut()
    {
        var subjectId = Guid.NewGuid();
        obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.RequestErasureAsync(subjectId);

        Assert.Equal(ErasureState.Completed, result.State);
        Assert.NotNull(result.CompletedAtUtc);
        userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        tenantModule.Verify(t => t.SeverMembershipsAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        conversationsModule.Verify(c => c.SeverAuthoredMessagesAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_LiveObligation_DefersWithoutAnonymising()
    {
        var subjectId = Guid.NewGuid();
        obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await service.RequestErasureAsync(subjectId);

        Assert.Equal(ErasureState.Deferred, result.State);
        Assert.NotNull(result.DeferralReason);
        Assert.Null(result.CompletedAtUtc);
        userModule.Verify(u => u.EraseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        tenantModule.Verify(t => t.SeverMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        conversationsModule.Verify(c => c.SeverAuthoredMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_NoObligations_ResolvesEmailBeforeErasingTheUserRow()
    {
        var subjectId = Guid.NewGuid();
        obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sequence = new List<string>();
        userModule.Setup(u => u.GetByIdAsync(subjectId))
            .ReturnsAsync(new UserDto { Id = subjectId, Email = "subject@test.invalid" })
            .Callback(() => sequence.Add("read-email"));
        userModule.Setup(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => sequence.Add("erase-user"));

        await service.RequestErasureAsync(subjectId);

        Assert.Equal(["read-email", "erase-user"], sequence);
    }
}
