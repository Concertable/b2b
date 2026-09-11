using Concertable.B2B.Application.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Application.Infrastructure.Data.Configurations;

internal sealed class ConcertAvailabilityEntityConfiguration : IEntityTypeConfiguration<ConcertAvailabilityEntity>
{
    public void Configure(EntityTypeBuilder<ConcertAvailabilityEntity> builder)
    {
        builder.ToTable(Schema.Tables.ConcertAvailabilities, Schema.Name);
        builder.HasKey(availability => availability.ConcertId);
        builder.Property(availability => availability.ConcertId).ValueGeneratedNever();
        builder.HasIndex(availability => availability.OpportunityId).IsUnique();
        builder.HasIndex(availability => new { availability.ArtistId, availability.StartDate });
        builder.HasIndex(availability => new { availability.VenueId, availability.StartDate });
    }
}
