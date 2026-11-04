using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsReadDbContext(
    DbContextOptions<ConversationsReadDbContext> options,
    ConversationsConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IConversationsReadDbContext
{
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Properties<Sha256Digest>()
            .HaveConversion<Sha256Digest.EfCoreValueConverter>()
            .HaveMaxLength(64)
            .AreFixedLength();
    }

    IQueryable<MessageEntity> IConversationsReadDbContext.Messages => base.Query<MessageEntity>();
}
