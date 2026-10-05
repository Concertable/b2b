using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ConcertCommandReceiptConfiguration : IEntityTypeConfiguration<ConcertCommandReceipt>
{
    public void Configure(EntityTypeBuilder<ConcertCommandReceipt> builder)
    {
        builder.ToTable(Schema.Tables.ConcertCommandReceipts, Schema.Name);
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Operation).IsRequired().HasMaxLength(64);
        builder.Property(receipt => receipt.IdempotencyHash)
            .HasConversion(hash => hash.Value, value => IdempotencyHash.From(value))
            .IsRequired()
            .HasMaxLength(64);
        builder.Property(receipt => receipt.Outcome).IsRequired().HasMaxLength(256);

        builder.HasIndex(receipt => new { receipt.IssuedByTenantId, receipt.Operation, receipt.RequestId })
            .IsUnique()
            .HasDatabaseName($"UX_{Schema.Tables.ConcertCommandReceipts}_Request");
    }
}
