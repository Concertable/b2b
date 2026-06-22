using Concertable.B2B.Concert.Domain.ReadModels;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ArtistReadModelConfiguration : IEntityTypeConfiguration<ArtistReadModel>
{
    public void Configure(EntityTypeBuilder<ArtistReadModel> builder)
    {
        builder.ToTable(Schema.Tables.ArtistReadModels, Schema.Name);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.HasIndex(a => a.UserId).IsUnique();
        builder.OwnsAddress(a => a.Address);
        builder.HasMany(a => a.Genres)
            .WithOne(g => g.Artist)
            .HasForeignKey(g => g.ArtistReadModelId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}

internal sealed class ArtistReadModelGenreConfiguration : IEntityTypeConfiguration<ArtistReadModelGenre>
{
    public void Configure(EntityTypeBuilder<ArtistReadModelGenre> builder)
    {
        builder.ToTable(Schema.Tables.ArtistReadModelGenres, Schema.Name);
        builder.HasKey(g => new { g.ArtistReadModelId, g.Genre });
    }
}
