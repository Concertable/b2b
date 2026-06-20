using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Tenant.Infrastructure.Data.Configurations;

internal sealed class TenantEntityConfiguration : IEntityTypeConfiguration<TenantEntity>
{
    public void Configure(EntityTypeBuilder<TenantEntity> builder)
    {
        builder.ToTable(Schema.Tables.Tenants, Schema.Name);
        builder.HasKey(o => o.Id);
        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Type).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.OwnsOne(o => o.Compliance, c =>
        {
            c.Property(x => x.VatNumber).HasMaxLength(20);
            c.Property(x => x.SellerIdentifier).IsRequired().HasMaxLength(50);
            c.Property(x => x.BankReference).IsRequired().HasMaxLength(50);

            c.OwnsOne(x => x.RegisteredAddress, a =>
            {
                a.Property(x => x.Line1).IsRequired().HasMaxLength(200);
                a.Property(x => x.Line2).HasMaxLength(200);
                a.Property(x => x.City).IsRequired().HasMaxLength(100);
                a.Property(x => x.Postcode).IsRequired().HasMaxLength(20);
                a.Property(x => x.Country).IsRequired().HasMaxLength(100);
            });
            c.Navigation(x => x.RegisteredAddress).IsRequired();
        });
    }
}
