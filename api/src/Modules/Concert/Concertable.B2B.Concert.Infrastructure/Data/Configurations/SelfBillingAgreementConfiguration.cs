using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class SelfBillingAgreementConfiguration : IEntityTypeConfiguration<SelfBillingAgreementEntity>
{
    public void Configure(EntityTypeBuilder<SelfBillingAgreementEntity> builder)
    {
        builder.ToTable(Schema.Tables.SelfBillingAgreements, Schema.Name);
        builder.Property(a => a.PlatformTermsVersion).HasMaxLength(64);
        builder.ComplexProperty(a => a.Supplier, InvoicePartyConfiguration.Configure);
        builder.ComplexProperty(a => a.SupplierESignature, ESignatureConfiguration.Configure);
    }
}
