using Concertable.B2B.Conversations.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Concertable.DataAccess.Infrastructure.Data;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new MessageEntityConfiguration());
    }
}
