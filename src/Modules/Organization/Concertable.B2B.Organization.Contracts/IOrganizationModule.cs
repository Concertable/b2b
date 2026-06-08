namespace Concertable.B2B.Organization.Contracts;

public interface IOrganizationModule
{
    Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
