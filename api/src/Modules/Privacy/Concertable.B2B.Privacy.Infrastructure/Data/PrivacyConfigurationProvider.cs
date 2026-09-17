using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Privacy.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Data;

internal sealed class PrivacyConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new SubjectErasureRequestConfiguration());
    }
}
