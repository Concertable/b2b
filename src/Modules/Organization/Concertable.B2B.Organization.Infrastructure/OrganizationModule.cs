namespace Concertable.B2B.Organization.Infrastructure;

internal sealed class OrganizationModule : IOrganizationModule
{
    private readonly IOrganizationService service;

    public OrganizationModule(IOrganizationService service)
    {
        this.service = service;
    }

    public Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct = default) =>
        service.GetByIdAsync(id, ct);
}
