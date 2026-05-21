using Microsoft.EntityFrameworkCore;

namespace Concertable.Conversations.Infrastructure.Data;

internal class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
