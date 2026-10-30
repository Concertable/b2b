using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Conversations, Schema.Name);
        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.CreatedAt).IsRequired();
        builder.Property(conversation => conversation.AccessVersion).IsConcurrencyToken();
        builder.HasMany(conversation => conversation.AccessGrants)
            .WithOne()
            .HasForeignKey(grant => grant.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
