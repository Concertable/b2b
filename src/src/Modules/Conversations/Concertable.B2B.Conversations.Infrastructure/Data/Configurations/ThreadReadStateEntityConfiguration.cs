using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ThreadReadStateEntityConfiguration : IEntityTypeConfiguration<ThreadReadStateEntity>
{
    public void Configure(EntityTypeBuilder<ThreadReadStateEntity> builder)
    {
        builder.ToTable(Schema.Tables.ThreadReadStates, Schema.Name);
        builder.HasIndex(t => new { t.VenueTenantId, t.ArtistTenantId, t.UserId }).IsUnique();
    }
}
