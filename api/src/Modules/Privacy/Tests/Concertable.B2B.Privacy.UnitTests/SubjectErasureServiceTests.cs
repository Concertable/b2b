using Concertable.B2B.Authorization.Contracts.Enums;
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
        this.repository.Setup(r => r.InsertAsync(It.IsAny<SubjectErasureRequestEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubjectErasureRequestEntity e, CancellationToken _) => e);
        this.userModule.Setup(u => u.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new UserDto { Id = Guid.NewGuid(), Email = "subject@test.invalid" });
        this.tenantModule.Setup(t => t.GetMembershipsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        this.tenantModule.Setup(t => t.SeverMembershipsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());

        this.service = new SubjectErasureService(
            this.repository.Object,
            this.obligationChecker.Object,
            this.userModule.Object,
            this.tenantModule.Object,
            this.conversationsModule.Object,
            TimeProvider.System,
            NullLogger<SubjectErasureService>.Instance);
    }

    [Fact]
    public async Task RequestErasureAsync_NoObligations_CompletesAndRunsTheFanOut()
    {
        var subjectId = Guid.NewGuid();
        this.obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

        Assert.Equal(ErasureState.Completed, result.State);
        Assert.NotNull(result.CompletedAtUtc);
        this.userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        this.tenantModule.Verify(t => t.SeverMembershipsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        this.conversationsModule.Verify(c => c.SeverAuthoredMessagesAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
        this.conversationsModule.Verify(c => c.ScrubParticipantProfilesAsync(It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()), Times.Once);
        this.tenantModule.Verify(t => t.PurgePendingInvitationsAsync("subject@test.invalid", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_LiveObligation_DefersWithoutAnonymising()
    {
        var subjectId = Guid.NewGuid();
        this.obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

        Assert.Equal(ErasureState.Deferred, result.State);
        Assert.NotNull(result.DeferralReason);
        Assert.Null(result.CompletedAtUtc);
        this.userModule.Verify(u => u.EraseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        this.tenantModule.Verify(t => t.SeverMembershipsAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
        this.conversationsModule.Verify(c => c.SeverAuthoredMessagesAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_NoObligations_ResolvesEmailBeforeErasingTheUserRow()
    {
        var subjectId = Guid.NewGuid();
        this.obligationChecker.Setup(g => g.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var sequence = new List<string>();
        this.userModule.Setup(u => u.GetByIdAsync(subjectId))
            .ReturnsAsync(new UserDto { Id = subjectId, Email = "subject@test.invalid" })
            .Callback(() => sequence.Add("read-email"));
        this.userModule.Setup(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Callback(() => sequence.Add("erase-user"));

        await this.service.RequestErasureAsync(subjectId);

        Assert.Equal(["read-email", "erase-user"], sequence);
    }

    [Fact]
    public async Task RequestErasureAsync_SubjectAlreadyHasARequest_ReDrivesItInsteadOfOpeningASecond()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        this.repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        this.obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var result));

        Assert.Equal(ErasureState.Completed, result.State);
        this.repository.Verify(
            r => r.InsertAsync(It.IsAny<SubjectErasureRequestEntity>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_DeferredSubjectWhoseObligationCleared_CompletesOnTheSecondPass()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        this.repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        this.obligationChecker.SetupSequence(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(false);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var deferred));
        Assert.Equal(ErasureState.Deferred, deferred.State);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var completed));
        Assert.Equal(ErasureState.Completed, completed.State);
        this.userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_StillObligatedOnASecondPass_StaysDeferredWithoutErasing()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        this.repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        this.obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await this.service.RequestErasureAsync(subjectId);
        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var again));

        Assert.Equal(ErasureState.Deferred, again.State);
        this.userModule.Verify(u => u.EraseAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RequestErasureAsync_AlreadyCompleted_IsTerminalAndDoesNotReRunTheFanOut()
    {
        var subjectId = Guid.NewGuid();
        var existing = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        this.repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        this.obligationChecker.Setup(o => o.HasLiveObligationsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await this.service.RequestErasureAsync(subjectId);
        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var again));

        Assert.Equal(ErasureState.Completed, again.State);
        this.userModule.Verify(u => u.EraseAsync(subjectId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestErasureAsync_MembershipRemovalFailsAfterCommit_RetryKeepsTheCapturedTenantScope()
    {
        var subjectId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var request = SubjectErasureRequestEntity.Create(subjectId, DateTime.UtcNow);
        this.repository.Setup(r => r.GetBySubjectIdAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync(request);
        this.tenantModule.Setup(t => t.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new MembershipDto(Guid.NewGuid(), tenantId, "Acme", TenantRole.Owner, 1, [], [])]);
        var capturedStateSaved = false;
        this.repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => capturedStateSaved |= request.TenantIds is not null)
            .Returns(Task.CompletedTask);
        var attempts = 0;
        this.tenantModule.Setup(t => t.SeverMembershipsAsync(subjectId, It.IsAny<IReadOnlySet<Guid>>(), It.IsAny<CancellationToken>()))
            .Returns((Guid _, IReadOnlySet<Guid> captured, CancellationToken _) =>
            {
                Assert.True(capturedStateSaved);
                Assert.Equal(new HashSet<Guid> { tenantId }, captured);
                return ++attempts == 1
                    ? Task.FromException<IReadOnlySet<Guid>>(new InvalidOperationException("Failure after membership commit"))
                    : Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid> { tenantId });
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => this.service.RequestErasureAsync(subjectId));
        this.tenantModule.Setup(t => t.GetMembershipsAsync(subjectId, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Assert.True((await this.service.RequestErasureAsync(subjectId)).TryGetValue(out var result));
        Assert.Equal(ErasureState.Completed, result.State);
        this.conversationsModule.Verify(c => c.ScrubParticipantProfilesAsync(
            It.Is<IReadOnlySet<Guid>>(ids => ids.SetEquals(new[] { tenantId })), It.IsAny<CancellationToken>()), Times.Once);
        this.userModule.Verify(u => u.GetByIdAsync(subjectId), Times.Once);
        Assert.Null(request.SubjectEmail);
        Assert.Null(request.TenantIds);
    }
}
