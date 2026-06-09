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
}
