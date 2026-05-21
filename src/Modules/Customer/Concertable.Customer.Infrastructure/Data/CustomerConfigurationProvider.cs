using Concertable.Customer.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Customer.Infrastructure.Data;

internal sealed class CustomerConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new PreferenceEntityConfiguration());
        modelBuilder.ApplyConfiguration(new GenrePreferenceEntityConfiguration());
    }
}
