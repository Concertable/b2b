namespace Concertable.B2B.Organization.Application.Interfaces;

internal interface IOrganizationRepository
{
    Task<OrganizationEntity?> GetByIdAsync(int id, CancellationToken ct = default);
    Task AddAsync(OrganizationEntity organization, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
