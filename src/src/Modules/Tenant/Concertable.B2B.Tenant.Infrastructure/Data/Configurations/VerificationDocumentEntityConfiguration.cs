using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class VerificationDocumentEntityConfiguration : IEntityTypeConfiguration<VerificationDocumentEntity>
{
    public void Configure(EntityTypeBuilder<VerificationDocumentEntity> builder)
    {
        builder.ToTable(Schema.Tables.VerificationDocuments, Schema.Name);
        builder.HasKey(d => d.Id);
        builder.Property(d => d.DocumentType).IsRequired();
        builder.Property(d => d.BlobName).IsRequired().HasMaxLength(500);
        builder.Property(d => d.UploadedAt).IsRequired();

        builder.HasIndex(d => d.TenantVerificationId);
    }
}
