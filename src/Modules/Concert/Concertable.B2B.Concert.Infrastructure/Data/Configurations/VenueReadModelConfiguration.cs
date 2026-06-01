using Concertable.B2B.Concert.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class VenueReadModelConfiguration : IEntityTypeConfiguration<VenueReadModel>
{
    public void Configure(EntityTypeBuilder<VenueReadModel> builder)
    {
        builder.ToTable(Schema.Tables.VenueReadModels, Schema.Name);
        builder.Property(v => v.Id).ValueGeneratedNever();
        builder.HasIndex(v => v.UserId).IsUnique();
        builder.Property(v => v.Location).HasColumnType("geography");
    }
}
