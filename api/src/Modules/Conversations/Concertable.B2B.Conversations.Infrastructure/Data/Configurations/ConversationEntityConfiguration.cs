using Concertable.B2B.DataAccess.Application;
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
        builder.OwnsMany(conversation => conversation.Participants, participant =>
        {
            participant.ToTable(Schema.Tables.ConversationParticipants, Schema.Name);
            participant.WithOwner().HasForeignKey("ConversationId");
            participant.HasKey("ConversationId", nameof(ConversationParticipant.TenantId));
            participant.HasIndex(row => row.TenantId);
        });
        builder.OwnsMany(conversation => conversation.Assignments, assignment =>
        {
            assignment.ToTable(Schema.Tables.ConversationAssignments, Schema.Name);
            assignment.WithOwner().HasForeignKey("ConversationId");
            assignment.HasKey("ConversationId", nameof(MemberAssignment.MembershipId));
            assignment.HasIndex(row => row.MembershipId);
        });
    }
}
