using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.Admin.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Admin.Infrastructure.Data;

internal sealed class AdminConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AdminProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdminInvitationEntityConfiguration());
    }
}
