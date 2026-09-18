using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable(Schema.Tables.Messages, Schema.Name);
        builder.HasOne<ThreadEntity>()
            .WithMany()
            .HasForeignKey(message => message.ThreadId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(message => new { message.ThreadId, message.SentDate });
        builder.HasIndex(message => message.SenderTenantId);
    }
}
