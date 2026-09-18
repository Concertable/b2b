using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider,
    ITenantContext tenantContext,
    IAccessContext accessContext)
    : AccessScopedDbContext(options, provider, tenantContext, accessContext, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ThreadEntity> Threads => Set<ThreadEntity>();
    public DbSet<ThreadAccessGrant> ThreadAccessGrants => Set<ThreadAccessGrant>();
    public DbSet<ThreadReadStateEntity> ThreadReadStates => Set<ThreadReadStateEntity>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();

    /* Messages and read state are reached through their thread; a report additionally requires that the
       reading tenant is the one that raised it, because being in a thread is not licence to read who
       reported whom or what a moderator concluded. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreadEntity>().HasQueryFilter(TenantFilters.Key, thread =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ThreadAccessGrants.Any(grant =>
                    grant.ResourceId == thread.Id
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.Entity<MessageEntity>().HasQueryFilter(TenantFilters.Key, message =>
            AccessContext.IsHost
            || (AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ThreadAccessGrants.Any(grant =>
                    grant.ResourceId == message.ThreadId
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.Entity<ThreadReadStateEntity>().HasQueryFilter(TenantFilters.Key, state =>
            AccessContext.IsHost
            || (state.TenantId == AccessContext.TenantId
                && state.UserId == AccessContext.UserId
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ThreadAccessGrants.Any(grant =>
                    grant.ResourceId == state.ThreadId
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));

        modelBuilder.Entity<ContentReportEntity>().HasQueryFilter(TenantFilters.Key, report =>
            AccessContext.IsHost
            || (report.ReporterTenantId == AccessContext.TenantId
                && AccessContext.UserId != null
                && MembershipAuthority.Any(membership =>
                    membership.TenantId == AccessContext.TenantId
                    && membership.UserId == AccessContext.UserId
                    && membership.AuthorizationVersion == AccessContext.AuthorizationVersion)
                && ThreadAccessGrants.Any(grant =>
                    grant.ResourceId == report.ThreadId
                    && grant.TenantId == AccessContext.TenantId
                    && (grant.MemberUserId == null || grant.MemberUserId == AccessContext.UserId)
                    && grant.RevokedAt == null
                    && grant.ValidFrom <= AccessContext.UtcNow
                    && (grant.ValidUntil == null || grant.ValidUntil > AccessContext.UtcNow))));
    }
}
