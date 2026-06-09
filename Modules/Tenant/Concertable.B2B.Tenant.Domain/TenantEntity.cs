using Concertable.Kernel;

namespace Concertable.B2B.Tenant.Domain;

public sealed class TenantEntity : IGuidEntity
{
    private TenantEntity() { }

    public Guid Id { get; private set; }
    public string LegalName { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static TenantEntity Create(string legalName, Guid createdByUserId, DateTime createdAt)
    {
        return new TenantEntity
        {
            Id = Guid.NewGuid(),
            LegalName = legalName,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }
}
