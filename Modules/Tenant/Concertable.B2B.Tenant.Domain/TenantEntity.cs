using Concertable.B2B.Tenant.Domain.Events;
using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

public sealed class TenantEntity : IGuidEntity, IEventRaiser
{
    private TenantEntity() { }

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// The legal/tax identity backing settlement and DAC7 reporting (<c>LEGAL_REQUIREMENTS.md</c> item 3).
    /// Null until the operator completes organization setup — provisioning creates the tenant bare.
    /// </summary>
    public Compliance? Compliance { get; private set; }

    private readonly EventRaiser events = new();
    public IReadOnlyList<IDomainEvent> DomainEvents => events.DomainEvents;
    public void ClearDomainEvents() => events.Clear();

    /// <summary>
    /// Creates a tenant and raises <see cref="TenantCreatedDomainEvent"/> so downstream services (Payment)
    /// provision off the resulting <c>TenantCreatedEvent</c>. <paramref name="id"/> lets seeders supply a
    /// deterministic id (so the event carries it, not a throwaway one); production omits it for a random id.
    /// </summary>
    public static TenantEntity Create(string legalName, Guid createdByUserId, DateTime createdAt, Guid? id = null)
    {
        var tenant = new TenantEntity
        {
            Id = id ?? Guid.NewGuid(),
            LegalName = legalName,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
        tenant.events.Raise(new TenantCreatedDomainEvent(tenant.Id, createdByUserId, legalName));
        return tenant;
    }

    /// <summary>
    /// Re-raises <see cref="TenantCreatedDomainEvent"/> for an already-persisted tenant. The dev/E2E seed
    /// inserts tenants directly (deterministic ids, so seeded rows link), so registration finds them present;
    /// announcing drives Payment provisioning through the same outbox path a fresh <see cref="Create"/> would.
    /// </summary>
    public void Announce() => events.Raise(new TenantCreatedDomainEvent(Id, CreatedByUserId, LegalName));

    /// <summary>
    /// Organization setup: replaces the provisioning placeholder legal name (the registration email)
    /// and the compliance details in one transition — the <c>/organizations</c> form submits them together.
    /// </summary>
    public void UpdateLegalDetails(string legalName, Compliance compliance)
    {
        DomainException.ThrowIfNullOrWhiteSpace(legalName, "Legal name");
        DomainException.ThrowIfNull(compliance, "Compliance");
        LegalName = legalName;
        Compliance = compliance;
    }
}
