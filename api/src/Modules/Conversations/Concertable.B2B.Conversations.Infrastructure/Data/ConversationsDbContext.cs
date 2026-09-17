using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Conversations.Contracts.Enums;
using Concertable.B2B.Conversations.Domain.ReadModels;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.Kernel.Identity;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Conversations.Infrastructure.Data;

internal sealed class ConversationsDbContext(
    DbContextOptions<ConversationsDbContext> options,
    ConversationsConfigurationProvider provider,
    ITenantContext tenantContext,
    IResourceAccessContext resourceAccess)
    : ResourceScopedDbContext(options, provider, tenantContext, resourceAccess, Schema.Name)
{
    public DbSet<ContentReportEntity> ContentReports => Set<ContentReportEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<ThreadEntity> Threads => Set<ThreadEntity>();
    public DbSet<ThreadAccessGrant> ThreadAccessGrants => Set<ThreadAccessGrant>();
    public DbSet<ThreadReadStateEntity> ThreadReadStates => Set<ThreadReadStateEntity>();
    public DbSet<ParticipantProfile> ParticipantProfiles => Set<ParticipantProfile>();

    public ResourceAudience ReadAudience => AudienceFor(TenantPermission.MessagesRead);
    public ResourceAudience SendAudience => AudienceFor(TenantPermission.MessagesSend);

    /* Messages and read state are reached through their conversation's Read grant; a report additionally
       requires that the reading tenant is the one that raised it, because being in a conversation is not
       licence to read who reported whom or what a moderator concluded. */
    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ThreadAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForCurrentMember<ThreadAccessGrant, ThreadAccessScope>(this)
                .And(grant =>
                    grant.Scope == ThreadAccessScope.Read
                        && (ReadAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || ReadAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)
                    || grant.Scope == ThreadAccessScope.SendMessages
                        && (SendAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || SendAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)));

        modelBuilder.Entity<ThreadEntity>().HasQueryFilter(TenantFilters.Key, thread =>
            ThreadAccessGrants.Any(grant =>
                grant.ResourceId == thread.Id && grant.Scope == ThreadAccessScope.Read));

        modelBuilder.Entity<MessageEntity>().HasQueryFilter(TenantFilters.Key, message =>
            ThreadAccessGrants.Any(grant =>
                grant.ResourceId == message.ThreadId && grant.Scope == ThreadAccessScope.Read));

        modelBuilder.Entity<ThreadReadStateEntity>().HasQueryFilter(TenantFilters.Key, state =>
            state.TenantId == ActiveTenantId
            && state.UserId == ActiveUserId
            && ThreadAccessGrants.Any(grant =>
                grant.ResourceId == state.ThreadId && grant.Scope == ThreadAccessScope.Read));

        modelBuilder.Entity<ContentReportEntity>().HasQueryFilter(TenantFilters.Key, report =>
            report.ReporterTenantId == ActiveTenantId
            && ThreadAccessGrants.Any(grant =>
                grant.ResourceId == report.ThreadId && grant.Scope == ThreadAccessScope.Read));
    }
}
