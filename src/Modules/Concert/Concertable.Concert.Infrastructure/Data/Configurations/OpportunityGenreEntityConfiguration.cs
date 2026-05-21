using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class OpportunityGenreEntityConfiguration : IEntityTypeConfiguration<OpportunityGenreEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityGenreEntity> builder)
    {
        builder.ToTable("OpportunityGenres", Schema.Name);
        builder.HasOne(og => og.Opportunity)
            .WithMany(o => o.OpportunityGenres)
            .HasForeignKey(og => og.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
