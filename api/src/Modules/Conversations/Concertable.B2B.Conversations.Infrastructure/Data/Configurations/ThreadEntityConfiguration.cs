using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.Conversations.Infrastructure.Data.Configurations;

internal sealed class ThreadEntityConfiguration : IEntityTypeConfiguration<ThreadEntity>
{
    public void Configure(EntityTypeBuilder<ThreadEntity> builder)
    {
        builder.ToTable(Schema.Tables.Threads, Schema.Name);
        builder.HasKey(thread => thread.Id);
        builder.Property(thread => thread.CreatedAt).IsRequired();
        builder.HasMany(thread => thread.AccessGrants)
            .WithOne()
            .HasForeignKey(grant => grant.ResourceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
