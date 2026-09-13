using Concertable.B2B.Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Application.Infrastructure.Data.Configurations;

internal sealed class VerifyPaymentEntityConfiguration : IEntityTypeConfiguration<VerifyPaymentEntity>
{
    public void Configure(EntityTypeBuilder<VerifyPaymentEntity> builder)
    {
        builder.ToTable(Schema.Tables.VerifyPayments, Schema.Name);
        builder.HasIndex(payment => payment.ApplicationId).IsUnique();
        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<SucceededVerifyPaymentEntity>(nameof(SucceededVerifyPaymentEntity))
            .HasValue<FailedVerifyPaymentEntity>(nameof(FailedVerifyPaymentEntity));
    }
}
