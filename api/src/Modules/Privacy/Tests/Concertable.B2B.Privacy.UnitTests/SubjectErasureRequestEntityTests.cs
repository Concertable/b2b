using Concertable.B2B.Privacy.Domain.Entities;
using Concertable.B2B.Privacy.Domain.Lifecycle;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class SubjectErasureRequestEntityTests
{
    [Fact]
    public void Create_NewRequest_StartsRequested()
    {
        var subjectId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var request = SubjectErasureRequestEntity.Create(subjectId, now);

        Assert.Equal(subjectId, request.SubjectId);
        Assert.Equal(ErasureState.Requested, request.State);
        Assert.Equal(now, request.RequestedAtUtc);
        Assert.Null(request.CompletedAtUtc);
    }

    [Fact]
    public void RecordCompletion_AfterDeferral_StampsCompletionAndClearsDeferralReason()
    {
        var request = SubjectErasureRequestEntity.Create(Guid.NewGuid(), DateTime.UtcNow);
        request.RecordDeferral("PendingFinancialObligations");
        var completedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);

        request.RecordCompletion(completedAt);

        Assert.Equal(completedAt, request.CompletedAtUtc);
        Assert.Null(request.DeferralReason);
    }

    [Fact]
    public void Fire_LegalTrigger_AdvancesState()
    {
        var request = SubjectErasureRequestEntity.Create(Guid.NewGuid(), DateTime.UtcNow);

        Assert.False(request.Fire(ErasureTrigger.Begin).TryGetError(out _));

        Assert.Equal(ErasureState.InProgress, request.State);
    }

    [Fact]
    public void Fire_IllegalTrigger_LeavesStateUntouched()
    {
        var request = SubjectErasureRequestEntity.Create(Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(request.Fire(ErasureTrigger.Complete).TryGetError(out _));

        Assert.Equal(ErasureState.Requested, request.State);
    }
}
