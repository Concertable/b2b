using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider)
    : DbContextBase(options)
{
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(Schema.Name);
        provider.Configure(modelBuilder);
    }
}
