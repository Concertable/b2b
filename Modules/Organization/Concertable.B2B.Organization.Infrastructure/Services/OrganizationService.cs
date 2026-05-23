namespace Concertable.B2B.Organization.Infrastructure.Services;

internal class OrganizationService : IOrganizationService
{
    private readonly IOrganizationRepository repository;

    public OrganizationService(IOrganizationRepository repository)
    {
        this.repository = repository;
    }

    public async Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var org = await repository.GetByIdAsync(id, ct);
        return org?.ToDto();
    }
}
