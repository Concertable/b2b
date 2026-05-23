using Concertable.B2B.Organization.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Organization.Infrastructure.Repositories;

internal class OrganizationRepository : IOrganizationRepository
{
    private readonly OrganizationDbContext dbContext;

    public OrganizationRepository(OrganizationDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public Task<OrganizationEntity?> GetByIdAsync(int id, CancellationToken ct = default) =>
        dbContext.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task AddAsync(OrganizationEntity organization, CancellationToken ct = default) =>
        await dbContext.Organizations.AddAsync(organization, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        dbContext.SaveChangesAsync(ct);
}
