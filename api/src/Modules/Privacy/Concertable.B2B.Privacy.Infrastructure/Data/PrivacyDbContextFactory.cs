using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Privacy.Infrastructure.Data;

internal sealed class PrivacyDbContextFactory : B2BDesignTimeDbContextFactory<PrivacyDbContext>
{
    protected override PrivacyDbContext Create(DbContextOptions<PrivacyDbContext> options) =>
        new(options, new PrivacyConfigurationProvider());
}
