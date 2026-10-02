using Concertable.B2B.DataAccess.Application;

namespace Concertable.B2B.Privacy.Domain.Entities;

public sealed class SubjectErasureRequestEntity : IGuidEntity, IConcurrencyVersioned
{
    private static readonly ErasureStateMachine stateMachine = new();

    private SubjectErasureRequestEntity() { }

    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public ErasureState State { get; private set; }
    public uint Version { get; private set; }
    public DateTime? LastAttemptedAtUtc { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? DeferralReason { get; private set; }
    public string? FailureReason { get; private set; }

    public string? SubjectEmail { get; private set; }
    public string? TenantIds { get; private set; }

    public static SubjectErasureRequestEntity Create(Guid subjectId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = subjectId,
        State = ErasureState.Requested,
        RequestedAtUtc = nowUtc,
    };

    internal UnitResult<ErasureTransitionError> Fire(ErasureTrigger trigger)
    {
        if (!stateMachine.Transition(this.State, trigger).TryGetValue(out var next))
            return new ErasureTransitionError.InvalidTransition(this.State, trigger);

        this.State = next;
        return new Success();
    }

    internal void RecordAttempt(DateTime at) => this.LastAttemptedAtUtc = at;

    internal void RecordDeferral(string reason) => this.DeferralReason = reason;

    internal void RecordCompletion(DateTime at)
    {
        this.CompletedAtUtc = at;
        this.DeferralReason = null;
        this.FailureReason = null;
        this.SubjectEmail = null;
        this.TenantIds = null;
    }

    internal void RecordFailure(string reason) => this.FailureReason = reason;

    internal void CaptureFanOutState(string? email, IReadOnlySet<Guid> tenantIds)
    {
        this.SubjectEmail ??= email;
        this.TenantIds ??= string.Join(",", tenantIds);
    }

    internal void RecordWoundDownTenants(IReadOnlySet<Guid> tenantIds) =>
        this.TenantIds = string.Join(",", tenantIds);

    internal IReadOnlySet<Guid> CapturedTenantIds =>
        string.IsNullOrEmpty(this.TenantIds)
            ? new HashSet<Guid>()
            : this.TenantIds.Split(",").Select(Guid.Parse).ToHashSet();
}
