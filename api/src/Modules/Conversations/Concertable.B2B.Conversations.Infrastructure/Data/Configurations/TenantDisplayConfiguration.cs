using Concertable.B2B.Conversations.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class TenantDisplayConfiguration : IEntityTypeConfiguration<TenantDisplay>
{
    public void Configure(EntityTypeBuilder<TenantDisplay> builder)
    {
        builder.ToTable(Schema.Tables.TenantDisplays, Schema.Name);
        builder.HasKey(display => display.TenantId);
        builder.Property(display => display.TenantId).ValueGeneratedNever();
        builder.Property(display => display.DisplayVersion).IsRequired();
        builder.Property(display => display.DisplayName).IsRequired().HasMaxLength(200);
    }
}
