using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ConversationCreationReceiptConfiguration
    : IEntityTypeConfiguration<ConversationCreationReceipt>
{
    public void Configure(EntityTypeBuilder<ConversationCreationReceipt> builder)
    {
        builder.ToTable(Schema.Tables.ConversationCreationReceipts, Schema.Name);
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.PayloadHash).IsRequired().HasMaxLength(64);
        builder.HasOne<ConversationEntity>()
            .WithMany()
            .HasForeignKey(receipt => receipt.ConversationId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(receipt => new
        {
            receipt.CreatorTenantId,
            receipt.CreatedByMembershipId,
            receipt.RequestId
        }).IsUnique();
    }
}
