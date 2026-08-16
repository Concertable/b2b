using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider,
    ITenantContext tenantContext)
    : VenueArtistTenantDbContext(options, provider, tenantContext, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ThreadReadStateEntity> ThreadReadStates => Set<ThreadReadStateEntity>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyVenueArtist<ContentReportEntity>(this);
        modelBuilder.ApplyVenueArtist<MessageEntity>(this);
        modelBuilder.ApplyVenueArtist<ThreadReadStateEntity>(this);
    }
}
