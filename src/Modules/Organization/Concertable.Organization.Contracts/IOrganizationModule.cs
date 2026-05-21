namespace Concertable.Organization.Contracts;

public interface IOrganizationModule
{
    Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
