using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure;

public abstract class ResourceCommandReceiptConfiguration<TReceipt> : IEntityTypeConfiguration<TReceipt>
    where TReceipt : ResourceCommandReceipt
{
    protected abstract string TableName { get; }
    protected abstract string SchemaName { get; }

    public void Configure(EntityTypeBuilder<TReceipt> builder)
    {
        builder.ToTable(TableName, SchemaName);
        builder.HasKey(receipt => receipt.Id);
        builder.Property(receipt => receipt.Operation).IsRequired().HasMaxLength(64);
        builder.Property(receipt => receipt.PayloadHash).IsRequired().HasMaxLength(64);
        builder.Property(receipt => receipt.Outcome).IsRequired().HasMaxLength(256);

        builder.HasIndex(receipt => new { receipt.IssuedByTenantId, receipt.Operation, receipt.RequestId })
            .IsUnique()
            .HasDatabaseName($"UX_{TableName}_Request");
    }
}
