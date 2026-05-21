using Concertable.DataAccess.Infrastructure;
using Concertable.Organization.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Organization.Infrastructure.Data;

internal sealed class OrganizationConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationEntityConfiguration());
    }
}
