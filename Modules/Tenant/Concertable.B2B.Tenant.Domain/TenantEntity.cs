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
    /// Creates a tenant from the operator's registration <paramref name="email"/> — the bare provisioning
    /// state before organization setup. The email seeds the placeholder <see cref="LegalName"/> and is carried
    /// on <see cref="TenantCreatedDomainEvent"/> as the Stripe account email, so downstream services (Payment)
    /// provision off the resulting <c>TenantCreatedEvent</c>. <paramref name="id"/> lets seeders supply a
    /// deterministic id (so the event carries it, not a throwaway one); production omits it for a random id.
    /// </summary>
    public static TenantEntity Create(string email, Guid createdByUserId, DateTime createdAt, Guid? id = null)
    {
        var tenant = new TenantEntity
        {
            Id = id ?? Guid.NewGuid(),
            LegalName = email,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
        tenant.events.Raise(new TenantCreatedDomainEvent(tenant.Id, createdByUserId, email));
        return tenant;
    }

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
