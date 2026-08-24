using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

/// <summary>
/// A tenant's evidence-backed legitimacy state — one row per tenant, absent until first submission. No
/// row means never submitted, the same fail-closed posture as a null <see cref="TenantEntity.TaxCompliance"/>.
/// Evidence is append-only: a submission or resubmission adds new <see cref="VerificationDocumentEntity"/>
/// rows rather than replacing prior ones, so the admin trail shows what was reviewed each time. <see cref="Approved"/>
/// is the only verified state; there is no legal edge back out of it.
/// </summary>
public sealed class TenantVerificationEntity : IGuidEntity, IEventRaiser
{
    private static readonly IStateMachine<TenantVerificationStatus, TenantVerificationTrigger> StateMachine =
        new StateMachine<TenantVerificationStatus, TenantVerificationTrigger>(
        [
            (TenantVerificationStatus.Pending, TenantVerificationTrigger.Approve, TenantVerificationStatus.Approved),
            (TenantVerificationStatus.Pending, TenantVerificationTrigger.Reject, TenantVerificationStatus.Rejected),
            (TenantVerificationStatus.Rejected, TenantVerificationTrigger.Resubmit, TenantVerificationStatus.Pending)
        ]);

    private readonly List<VerificationDocumentEntity> documents = [];
    private readonly EventRaiser events = new();

    private TenantVerificationEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public TenantVerificationStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public Guid? ReviewedByAdminSub { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    public IReadOnlyList<VerificationDocumentEntity> Documents => documents.AsReadOnly();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    /// <summary>First submission for a tenant. There is no "no row" state in <see cref="TenantVerificationStatus"/> —
    /// the state machine governs only edges between existing rows, so the initial row is created directly in
    /// <see cref="TenantVerificationStatus.Pending"/> rather than through a transition.</summary>
    public static TenantVerificationEntity Submit(Guid tenantId, IReadOnlyCollection<VerificationDocumentEntity> evidence, DateTime submittedAt)
    {
        if (evidence is null || evidence.Count == 0)
            throw new DomainException("At least one evidence document is required to submit for verification.");

        var verification = new TenantVerificationEntity
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = TenantVerificationStatus.Pending,
            SubmittedAt = submittedAt,
        };
        verification.documents.AddRange(evidence);
        verification.Announce();
        return verification;
    }

    /// <summary>Re-submission after a rejection: appends new evidence, clears the prior review, and returns to
    /// <see cref="TenantVerificationStatus.Pending"/>. Only legal from <see cref="TenantVerificationStatus.Rejected"/>.</summary>
    public void Resubmit(IReadOnlyCollection<VerificationDocumentEntity> evidence, DateTime submittedAt)
    {
        if (evidence is null || evidence.Count == 0)
            throw new DomainException("At least one evidence document is required to resubmit for verification.");

        Apply(TenantVerificationTrigger.Resubmit);
        documents.AddRange(evidence);
        RejectionReason = null;
        ReviewedByAdminSub = null;
        ReviewedAt = null;
        SubmittedAt = submittedAt;
        Announce();
    }

    /// <summary>Only legal from <see cref="TenantVerificationStatus.Pending"/>.</summary>
    public void Approve(Guid adminSub, DateTime approvedAt)
    {
        Apply(TenantVerificationTrigger.Approve);
        ReviewedByAdminSub = adminSub;
        ReviewedAt = approvedAt;
        RejectionReason = null;
        Announce();
    }

    /// <summary>Only legal from <see cref="TenantVerificationStatus.Pending"/>.</summary>
    public void Reject(Guid adminSub, string reason, DateTime rejectedAt)
    {
        DomainException.ThrowIfNullOrWhiteSpace(reason, "RejectionReason");
        if (reason.Length > 1000)
            throw new DomainException("RejectionReason must be 1000 characters or fewer.");

        Apply(TenantVerificationTrigger.Reject);
        ReviewedByAdminSub = adminSub;
        ReviewedAt = rejectedAt;
        RejectionReason = reason;
        Announce();
    }

    private void Apply(TenantVerificationTrigger trigger)
    {
        if (!StateMachine.Transition(Status, trigger).TryGetValue(out var next))
            throw new DomainException($"Cannot apply '{trigger}' to a verification in status '{Status}'.");

        Status = next;
    }

    private void Announce() =>
        events.Raise(new TenantVerificationChangedDomainEvent(
            Id, TenantId, Status, RejectionReason, ReviewedByAdminSub, ReviewedAt));
}
