using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Organization.Infrastructure.Data.Configurations;

internal sealed class OrganizationEntityConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable(Schema.Tables.Organizations, Schema.Name);
        builder.HasKey(o => o.Id);
        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
