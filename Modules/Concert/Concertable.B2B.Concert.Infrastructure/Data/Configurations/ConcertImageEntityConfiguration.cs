using Concertable.B2B.Concert.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal class ConcertImageEntityConfiguration : IEntityTypeConfiguration<ConcertImageEntity>
{
    public void Configure(EntityTypeBuilder<ConcertImageEntity> builder)
    {
        builder.ToTable(Schema.Tables.ConcertImages, Schema.Name);
        builder.HasOne(ci => ci.Concert)
            .WithMany(c => c.Images)
            .HasForeignKey(ci => ci.ConcertId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
