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
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<ConversationAccessGrant> ConversationAccessGrants => Set<ConversationAccessGrant>();
    public DbSet<ConversationReadPosition> ConversationReadPositions => Set<ConversationReadPosition>();
    public DbSet<TenantDisplay> TenantDisplays => Set<TenantDisplay>();

    public ResourceAudience ReadAudience => AudienceFor(TenantPermission.MessagesRead);
    public ResourceAudience SendAudience => AudienceFor(TenantPermission.MessagesSend);

    protected override void ApplyTenantFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConversationAccessGrant>().HasQueryFilter(TenantFilters.Key,
            ResourceAccessExpressions.LiveForCurrentMember<ConversationAccessGrant, ConversationAccessScope>(this)
                .And(grant =>
                    grant.Scope == ConversationAccessScope.Read
                        && (ReadAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || ReadAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)
                    || grant.Scope == ConversationAccessScope.SendMessages
                        && (SendAudience == ResourceAudience.TenantResources
                                && (grant.MembershipId == null || grant.MembershipId == ActiveMembershipId)
                            || SendAudience == ResourceAudience.AssignedResources
                                && grant.MembershipId == ActiveMembershipId)));

        modelBuilder.Entity<ConversationEntity>().HasQueryFilter(TenantFilters.Key, conversation =>
            ConversationAccessGrants.Any(grant =>
                grant.ResourceId == conversation.Id && grant.Scope == ConversationAccessScope.Read));

        modelBuilder.Entity<MessageEntity>().HasQueryFilter(TenantFilters.Key, message =>
            ConversationAccessGrants.Any(grant =>
                grant.ResourceId == message.ConversationId && grant.Scope == ConversationAccessScope.Read));

        modelBuilder.Entity<ConversationReadPosition>().HasQueryFilter(TenantFilters.Key, state =>
            state.TenantId == ActiveTenantId
            && state.MembershipId == ActiveMembershipId
            && ConversationAccessGrants.Any(grant =>
                grant.ResourceId == state.ConversationId && grant.Scope == ConversationAccessScope.Read));

        modelBuilder.Entity<ContentReportEntity>().HasQueryFilter(TenantFilters.Key, report =>
            report.ReporterTenantId == ActiveTenantId
            && report.ReporterUserId == ActiveUserId
            && ConversationAccessGrants.Any(grant =>
                grant.ResourceId == report.ConversationId && grant.Scope == ConversationAccessScope.Read));
    }
}
