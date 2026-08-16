using Concertable.Kernel;

namespace Concertable.B2B.Conversations.Domain.ReadModels;

public sealed class ParticipantProfile
{
    private ParticipantProfile() { }

    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public Address Address { get; private set; } = null!;

    public static ParticipantProfile Create(Guid tenantId, string name, string county, string town) => new()
    {
        TenantId = tenantId,
        Name = name,
        Address = new Address(county, town)
    };

    public void Update(string name, string county, string town)
    {
        Name = name;
        Address = new Address(county, town);
    }
}
