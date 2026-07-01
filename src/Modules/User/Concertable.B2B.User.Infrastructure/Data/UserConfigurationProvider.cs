using Concertable.DataAccess.Infrastructure.Data;
using Concertable.B2B.User.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
        modelBuilder.ApplyConfiguration(new VenueManagerProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ArtistManagerProfileEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AdminProfileEntityConfiguration());
    }
}
