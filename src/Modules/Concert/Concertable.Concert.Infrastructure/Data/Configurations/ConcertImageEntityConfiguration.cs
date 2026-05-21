using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class ConcertImageEntityConfiguration : IEntityTypeConfiguration<ConcertImageEntity>
{
    public void Configure(EntityTypeBuilder<ConcertImageEntity> builder)
    {
        builder.ToTable("ConcertImages", Schema.Name);
        builder.HasOne(ci => ci.Concert)
            .WithMany(c => c.Images)
            .HasForeignKey(ci => ci.ConcertId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
