namespace Concertable.B2B.Privacy.Domain.Entities;

/// <summary>
/// The durable record that a GDPR erasure was raised for a data subject, carried through the
/// <see cref="ErasureState"/> machine (Requested → Deferred/InProgress → Completed, plus Failed). It is the
/// evidence an ICO enquiry would ask for, and — because a Deferred request is re-drivable — the anchor the
/// hourly sweep re-evaluates until the subject's last financial obligation settles. Keyed by its own id;
/// <see cref="SubjectId"/> is the Auth <c>sub</c> being erased.
/// </summary>
public sealed class SubjectErasureRequestEntity : IGuidEntity
{
    private SubjectErasureRequestEntity() { }

    public Guid Id { get; private set; }
    public Guid SubjectId { get; private set; }
    public ErasureState State { get; private set; }
    public DateTime RequestedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public string? DeferralReason { get; private set; }
    public string? FailureReason { get; private set; }

    public static SubjectErasureRequestEntity Create(Guid subjectId, DateTime nowUtc) => new()
    {
        Id = Guid.NewGuid(),
        SubjectId = subjectId,
        State = ErasureState.Requested,
        RequestedAtUtc = nowUtc,
    };

    internal void Transition(ErasureState next) => State = next;

    internal void RecordDeferral(string reason) => DeferralReason = reason;

    internal void RecordCompletion(DateTime at)
    {
        CompletedAtUtc = at;
        DeferralReason = null;
    }

    internal void RecordFailure(string reason) => FailureReason = reason;
}
