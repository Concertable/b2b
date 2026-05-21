using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Conversations.Infrastructure.Data.Configurations;

internal class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable("Messages", Schema.Name);
        builder.HasIndex(m => m.ToUserId);
        builder.HasIndex(m => m.FromUserId);
    }
}
