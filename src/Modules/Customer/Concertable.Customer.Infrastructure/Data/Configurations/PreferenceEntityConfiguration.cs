using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Customer.Infrastructure.Data.Configurations;

internal class PreferenceEntityConfiguration : IEntityTypeConfiguration<PreferenceEntity>
{
    public void Configure(EntityTypeBuilder<PreferenceEntity> builder)
    {
        builder.ToTable("Preferences", Schema.Name);
        builder.HasIndex(p => p.UserId).IsUnique();
    }
}
