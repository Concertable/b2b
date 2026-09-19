using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantBusinessActivityEntity : IGuidEntity
{
    private TenantBusinessActivityEntity() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public TenantBusinessActivityKind Kind { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? RetiredAt { get; private set; }

    public bool IsActive => RetiredAt is null;

    internal static TenantBusinessActivityEntity Create(Guid tenantId, TenantBusinessActivityKind kind, DateTime at) =>
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
