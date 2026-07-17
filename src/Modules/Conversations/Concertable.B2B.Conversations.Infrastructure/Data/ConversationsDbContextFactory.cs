using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContextFactory : B2BDesignTimeDbContextFactory<ConversationsDbContext>
{
    protected override ConversationsDbContext Create(DbContextOptions<ConversationsDbContext> options) =>
        new(options, new ConversationsConfigurationProvider());
}
