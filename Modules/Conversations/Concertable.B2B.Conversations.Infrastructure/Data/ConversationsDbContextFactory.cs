using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal class ConversationsDbContextFactory : IDesignTimeDbContextFactory<ConversationsDbContext>
{
    public ConversationsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__B2BDb")
            ?? "Server=localhost,1433;Database=concertable-b2b;User Id=sa;Password=Password11!;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ConversationsDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ConversationsDbContext(options, new ConversationsConfigurationProvider());
    }
}
