namespace Concertable.Organization.Domain;

public class OrganizationEntity
{
    private OrganizationEntity() { }

    public int Id { get; private set; }
    public string LegalName { get; private set; } = null!;
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public static OrganizationEntity Create(string legalName, Guid createdByUserId, DateTime createdAt)
    {
        return new OrganizationEntity
        {
            LegalName = legalName,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }
}
