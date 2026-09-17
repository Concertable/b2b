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
            .ReturnsAsync(new HashSet<Guid>());

        this.service = new SubjectErasureService(
            repository.Object,
            obligationChecker.Object,
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

        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

        Assert.Equal(ErasureState.Completed, result.State);
        Assert.NotNull(result.CompletedAtUtc);
        userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        tenantModule.Verify(t => t.SeverMembershipsAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        conversationsModule.Verify(c => c.SeverAuthoredMessagesAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        conversationsModule.Verify(c => c.ScrubParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        tenantModule.Verify(t => t.PurgePendingInvitationsAsync("subject@test.invalid", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_LiveObligation_DefersWithoutAnonymising()
    {
        var subjectId = Guid.NewGuid();
        obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

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

    [Fact]
    public async Task RequestErasureAsync_SubjectAlreadyHasARequest_ReDrivesItInsteadOfOpeningASecond()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

        Assert.Equal(ErasureState.Completed, result.State);
        repository.Verify(
            r => r.InsertAsync(It.IsAny<SubjectErasureRequestEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_DeferredSubjectWhoseObligationCleared_CompletesOnTheSecondPass()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        obligationChecker.SetupSequence(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var deferred));
        Assert.Equal(ErasureState.Deferred, deferred.State);

        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var completed));
        Assert.Equal(ErasureState.Completed, completed.State);
        userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_StillObligatedOnASecondPass_StaysDeferredWithoutErasing()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await service.RequestErasureAsync(subjectId);
        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var again));

        Assert.Equal(ErasureState.Deferred, again.State);
        userModule.Verify(u => u.EraseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_AlreadyCompleted_IsTerminalAndDoesNotReRunTheFanOut()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await service.RequestErasureAsync(subjectId);
        Assert.True((await service.RequestErasureAsync(subjectId)).TryGetValue(out var again));

        Assert.Equal(ErasureState.Completed, again.State);
        userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
