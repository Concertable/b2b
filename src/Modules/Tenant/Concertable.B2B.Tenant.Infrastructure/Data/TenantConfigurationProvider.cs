using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Tenant.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Tenant.Infrastructure.Data;

internal sealed class TenantConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TenantEntityConfiguration());
        modelBuilder.ApplyConfiguration(new TenantMembershipEntityConfiguration());
    }
}
