using Concertable.Concert.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.Concert.Infrastructure.Data.Configurations;

internal class OpportunityEntityConfiguration : IEntityTypeConfiguration<OpportunityEntity>
{
    public void Configure(EntityTypeBuilder<OpportunityEntity> builder)
    {
        builder.ToTable("Opportunities", Schema.Name);
        builder.OwnsOne(o => o.Period, p =>
        {
            p.Property(x => x.Start).HasColumnName("StartDate");
            p.Property(x => x.End).HasColumnName("EndDate");
        });
        builder.HasOne(o => o.Venue)
            .WithMany()
            .HasForeignKey(o => o.VenueId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.Property(o => o.ContractId).IsRequired();
        builder.HasIndex(o => o.ContractId).IsUnique();
    }
}

internal class ApplicationEntityConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("Applications", Schema.Name);
        builder.HasIndex(ca => new { ca.OpportunityId, ca.ArtistId }).IsUnique();
        builder.HasOne(ca => ca.Opportunity)
            .WithMany(o => o.Applications)
            .HasForeignKey(ca => ca.OpportunityId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasOne(ca => ca.Artist)
            .WithMany()
            .HasForeignKey(ca => ca.ArtistId)
            .IsRequired()
            .OnDelete(DeleteBehavior.NoAction);
        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<StandardApplication>(nameof(StandardApplication))
            .HasValue<PrepaidApplication>(nameof(PrepaidApplication));
    }
}
