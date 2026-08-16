using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ContentReportEntityConfiguration : IEntityTypeConfiguration<ContentReportEntity>
{
    public void Configure(EntityTypeBuilder<ContentReportEntity> builder)
    {
        builder.ToTable(Schema.Tables.ContentReports, Schema.Name);
        builder.Property(r => r.MessageExcerpt).HasMaxLength(ContentReportEntity.MaxExcerptLength);
        builder.Property(r => r.Category).HasConversion<string>();
        builder.Property(r => r.Outcome).HasConversion<string>();
        builder.HasIndex(r => r.MessageId);
        builder.HasIndex(r => r.VenueTenantId);
        builder.HasIndex(r => r.ArtistTenantId);
    }
}
