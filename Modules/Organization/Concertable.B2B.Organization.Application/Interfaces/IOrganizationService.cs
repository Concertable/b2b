namespace Concertable.B2B.Organization.Application.Interfaces;

internal interface IOrganizationService
{
    Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
