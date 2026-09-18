using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

/// <summary>
/// One kind of marketplace work a tenant has activated, unique per <c>(TenantId, Kind)</c>. Retiring a
/// profile ends eligibility for new work of that kind; it does not touch an existing agreement's identity or
/// settlement rights, which were fixed when that agreement was accepted.
/// </summary>
public sealed class TenantBusinessProfileEntity : IGuidEntity
{
    private TenantBusinessProfileEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public TenantBusinessProfileKind Kind { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? RetiredAt { get; private set; }

    public bool IsActive => RetiredAt is null;

    internal static TenantBusinessProfileEntity Create(Guid tenantId, TenantBusinessProfileKind kind, DateTime at) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Kind = kind,
            ActivatedAt = at,
        };

    internal void Retire(DateTime at) => RetiredAt = at;

    internal void Reactivate(DateTime at)
    {
        ActivatedAt = at;
        RetiredAt = null;
    }
}
