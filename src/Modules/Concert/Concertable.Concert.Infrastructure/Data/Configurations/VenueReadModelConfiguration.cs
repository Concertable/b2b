using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class VenueReadModelConfiguration : IEntityTypeConfiguration<VenueReadModel>
{
    public void Configure(EntityTypeBuilder<VenueReadModel> builder)
    {
        builder.ToTable("VenueReadModels", Schema.Name);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.HasIndex(v => v.UserId).IsUnique();
        builder.Property(v => v.Location).HasColumnType("geography");
    }
}
