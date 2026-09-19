using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ConversationReadPositionConfiguration : IEntityTypeConfiguration<ConversationReadPosition>
{
    public void Configure(EntityTypeBuilder<ConversationReadPosition> builder)
    {
        builder.ToTable(Schema.Tables.ConversationReadPositions, Schema.Name);
        builder.HasOne<ConversationEntity>()
            .WithMany()
            .HasForeignKey(state => state.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(position => new { position.ConversationId, position.MembershipId }).IsUnique();
        builder.HasIndex(position => new { position.TenantId, position.MembershipId });
    }
}
