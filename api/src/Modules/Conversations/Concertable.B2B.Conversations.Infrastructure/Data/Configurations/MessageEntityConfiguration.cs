using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class MessageEntityConfiguration : IEntityTypeConfiguration<MessageEntity>
{
    public void Configure(EntityTypeBuilder<MessageEntity> builder)
    {
        builder.ToTable(Schema.Tables.Messages, Schema.Name);
        builder.HasOne<ConversationEntity>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(message => message.PayloadHash).IsRequired().HasMaxLength(64);
        builder.Property(message => message.Content).IsRequired();
        builder.HasIndex(message => new { message.ConversationId, message.Sequence }).IsUnique();
        builder.HasIndex(message => new { message.ConversationId, message.SentByMembershipId, message.RequestId }).IsUnique();
        builder.HasIndex(message => new { message.ConversationId, message.SentAt });
        builder.HasIndex(message => message.SenderTenantId);
    }
}
