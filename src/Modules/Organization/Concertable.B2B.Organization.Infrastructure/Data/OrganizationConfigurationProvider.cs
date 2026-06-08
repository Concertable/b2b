using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Organization.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Organization.Infrastructure.Data;

internal sealed class OrganizationConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OrganizationEntityConfiguration());
    }
}
