using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider,
    ITenantContext tenantContext)
    : VenueArtistTenantDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ThreadReadStateEntity> ThreadReadStates => Set<ThreadReadStateEntity>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVenueArtist<MessageEntity>(this);
        modelBuilder.ApplyVenueArtist<ThreadReadStateEntity>(this);
    }
}
