using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class InvoiceSequenceEntityConfiguration : IEntityTypeConfiguration<InvoiceSequenceEntity>
{
    public void Configure(EntityTypeBuilder<InvoiceSequenceEntity> builder)
    {
        builder.ToTable(Schema.Tables.InvoiceSequences, Schema.Name);

        builder.HasKey(s => s.TenantId);
        builder.Property(s => s.TenantId).ValueGeneratedNever();
        builder.Property(s => s.RowVersion).IsRowVersion();
    }
}
