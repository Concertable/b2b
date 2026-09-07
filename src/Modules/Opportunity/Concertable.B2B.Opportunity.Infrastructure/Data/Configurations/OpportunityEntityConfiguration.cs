using Concertable.B2B.Opportunity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Opportunity.Infrastructure.Data.Configurations;

internal sealed class OpportunityEntityConfiguration : IEntityTypeConfiguration<OpportunityEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityEntity> builder)
    {
        builder.ToTable(Schema.Tables.Opportunities, Schema.Name);
        builder.ComplexProperty(o => o.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("StartDate");
            p.Property(x => x.End).HasColumnName("EndDate");
        });
        builder.Property(o => o.VenueId).IsRequired();
        builder.Property(o => o.DealId).IsRequired();
        builder.Property(o => o.State).IsRequired();
        builder.HasIndex(o => o.DealId).IsUnique();
        builder.PrimitiveCollection(o => o.Genres);
    }
}
