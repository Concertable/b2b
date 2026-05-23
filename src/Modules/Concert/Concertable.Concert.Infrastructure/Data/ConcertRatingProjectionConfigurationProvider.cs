using Concertable.Concert.Domain.ReadModels;
using Concertable.Concert.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Concert.Infrastructure.Data;

internal sealed class ConcertRatingProjectionConfigurationProvider : IRatingProjectionConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ConcertRatingProjectionConfiguration());
        modelBuilder.Entity<ConcertRatingProjection>().ToTable(t => t.ExcludeFromMigrations());
    }
}
