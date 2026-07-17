using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.User.Infrastructure.Data;

internal sealed class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseSqlServer(connectionString, o => o.UseNetTopologySuite())
            .Options;
        return new UserDbContext(options, new UserConfigurationProvider());
    }
}
