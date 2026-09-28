using Concertable.B2B.DataAccess.Application;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;
using DisplayNames = Concertable.B2B.Application.Contracts.DisplayNames;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationRepository repository;
    private readonly IApplicationReadDbContext readDbContext;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;
    private readonly ITenantModule tenantModule;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly IOpportunityModule opportunityModule;
    private readonly IVenueModule venueModule;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;

    public ApplicationNotifier(
        IApplicationRepository repository,
        IApplicationReadDbContext readDbContext,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient,
        ITenantModule tenantModule,
        IPermissionCatalog permissionCatalog,
        IOpportunityModule opportunityModule,
        IVenueModule venueModule,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork)
    {
        this.repository = repository;
        this.readDbContext = readDbContext;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
        this.tenantModule = tenantModule;
        this.permissionCatalog = permissionCatalog;
        this.opportunityModule = opportunityModule;
        this.venueModule = venueModule;
        this.unitOfWork = unitOfWork;
    }

    public async Task VerifyPaymentFailedAsync(int applicationId, string failureMessage)
    {
        var application = await readDbContext.Applications
            .Where(value => value.Id == applicationId)
            .Select(value => new { value.OpportunityId, value.VenueTenantId })
            .SingleOrDefaultAsync();
        if (application is null)
            throw new NotFoundException(DisplayNames.Application);
        var opportunity = await opportunityModule.GetAsync(application.OpportunityId);
        if (!opportunity.TryGetValue(out var value))
            return;

        var venue = await venueModule.GetProfileAsync(value.VenueId);
        if (!venue.TryGetValue(out var profile))
            return;

        await unitOfWork.ExecuteAsync(async () =>
        {
            var eligible = (await tenantModule.GetCurrentMembershipsAsync([application.VenueTenantId]))
                .Where(membership => permissionCatalog.Grants(membership.Role, TenantPermission.ApplicationsDecide))
                .OrderBy(membership => membership.UserId == profile.UserId
                    ? 0
                    : membership.Role == TenantRole.Owner ? 1 : 2)
                .ThenBy(membership => membership.UserId);
            foreach (var candidate in eligible)
            {
                var recipient = await tenantModule.RequireCurrentMembershipAsync(candidate);
                if (recipient is null
                    || !permissionCatalog.Grants(recipient.Role, TenantPermission.ApplicationsDecide))
                    continue;

                await notificationClient.SendAsync(
                    recipient.UserId.ToString(),
                    "VerifyPaymentFailed",
                    new { applicationId, failureMessage });
                return;
            }
        });
    }

    public Task AppliedAsync(int applicationId) =>
        NotifyVenueAsync(
            applicationId,
            $"{currentUser.Email} has applied to your concert opportunity",
            MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(int applicationId) =>
        NotifyVenueAsync(
            applicationId,
            $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application has been accepted!",
            MessageAction.ApplicationAccepted);

    public Task RejectedAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application was not selected for this concert opportunity",
            MessageAction.ApplicationRejected);

    public Task CancelledAsync(int applicationId) =>
        NotifyArtistAsync(
            applicationId,
            "Your application was cancelled by the venue",
            MessageAction.ApplicationCancelled);

    private async Task NotifyVenueAsync(
        int applicationId,
        string content,
        MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository
            .GetByIdAsync(applicationId, VenueArtistTenantSpecification<ApplicationEntity>.CreatePair())
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAsync(
            venueTenantId,
            artistTenantId,
            artistTenantId,
            currentUser.GetId(),
            content,
            action);
    }

    private async Task NotifyArtistAsync(
        int applicationId,
        string content,
        MessageAction action)
    {
        var (venueTenantId, artistTenantId) = await repository
            .GetByIdAsync(applicationId, VenueArtistTenantSpecification<ApplicationEntity>.CreatePair())
            .OrNotFound(DisplayNames.Application);

        await conversationsModule.SendAndNotifyAsync(
            venueTenantId,
            artistTenantId,
            venueTenantId,
            currentUser.GetId(),
            content,
            action);
    }
}
