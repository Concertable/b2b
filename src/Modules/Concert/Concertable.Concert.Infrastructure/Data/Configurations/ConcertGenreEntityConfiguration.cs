using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class ConcertGenreEntityConfiguration : IEntityTypeConfiguration<ConcertGenreEntity>
{
    public void Configure(EntityTypeBuilder<ConcertGenreEntity> builder)
    {
        builder.ToTable("ConcertGenres", Schema.Name);
        builder.HasOne(cg => cg.Concert)
            .WithMany(c => c.ConcertGenres)
            .HasForeignKey(cg => cg.ConcertId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
