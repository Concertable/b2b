using Concertable.DataAccess.Infrastructure;
using Concertable.Conversations.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Concertable.Conversations.Infrastructure.Data;

internal sealed class ConversationsConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageEntityConfiguration());
    }
}
