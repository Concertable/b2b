using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.Kernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ParticipantProfileConfiguration : IEntityTypeConfiguration<ParticipantProfile>
{
    public void Configure(EntityTypeBuilder<ParticipantProfile> builder)
    {
        builder.ToTable(Schema.Tables.ParticipantProfiles, Schema.Name);
        builder.HasKey(p => p.TenantId);
        builder.Property(p => p.TenantId).ValueGeneratedNever();
        builder.OwnsAddress(p => p.Address);
    }
}
