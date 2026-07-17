using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContextFactory : IDesignTimeDbContextFactory<ConversationsDbContext>
{
    public ConversationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = DesignTimeConnectionString.B2B();
        var options = new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ConversationsDbContext(options, new ConversationsConfigurationProvider());
    }
}
