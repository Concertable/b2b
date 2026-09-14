using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Privacy.Infrastructure.Data.Configurations;

internal sealed class SubjectErasureRequestConfiguration : IEntityTypeConfiguration<SubjectErasureRequestEntity>
{
    public void Configure(EntityTypeBuilder<SubjectErasureRequestEntity> builder)
    {
        builder.ToTable(Schema.Tables.SubjectErasureRequests, Schema.Name);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.State);
        builder.Property(x => x.DeferralReason).HasMaxLength(200);
        builder.Property(x => x.FailureReason).HasMaxLength(1000);
        builder.HasIndex(x => x.SubjectId);
    }
}
