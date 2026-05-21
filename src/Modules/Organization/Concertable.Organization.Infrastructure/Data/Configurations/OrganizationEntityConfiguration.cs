using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Organization.Infrastructure.Data.Configurations;

internal class OrganizationEntityConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable("Organizations");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.LegalName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
