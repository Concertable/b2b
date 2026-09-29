using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Authorization.Contracts.Enums;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private readonly IApplicationPrivilegedRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;
    private readonly IOpportunityPrivilegedRepository opportunityRepository;
    private readonly IVenuePrivilegedRepository venueRepository;
    private readonly ITenantModule tenantModule;
    private readonly IPermissionCatalog permissionCatalog;
    private readonly ICommandExecutor commandExecutor;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;

    public ApplicationNotifier(
        IApplicationPrivilegedRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient,
        IOpportunityPrivilegedRepository opportunityRepository,
        IVenuePrivilegedRepository venueRepository,
        ITenantModule tenantModule,
        IPermissionCatalog permissionCatalog,
        ICommandExecutor commandExecutor,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
        this.opportunityRepository = opportunityRepository;
        this.venueRepository = venueRepository;
        this.tenantModule = tenantModule;
        this.permissionCatalog = permissionCatalog;
        this.commandExecutor = commandExecutor;
        this.unitOfWork = unitOfWork;
    }

    public async Task VerifyPaymentFailedAsync(int applicationId, string failureMessage)
    {
        var venueTenantId = await repository.GetVenueTenantIdAsync(applicationId);
        if (venueTenantId is null)
            return;

        var venueCreatorUserId = await commandExecutor.ExecuteAsync<ApplicationNotifier, Guid?>(
            (notifier, ct) => notifier.GetVenueCreatorUserIdAsync(applicationId, ct));
        if (venueCreatorUserId is null)
            return;

        await unitOfWork.ExecuteAsync(async () =>
        {
            var eligible = (await tenantModule.GetCurrentMembershipsForNotificationAsync(venueTenantId.Value))
                .Where(membership => permissionCatalog.Grants(membership.Role, TenantPermission.ApplicationsDecide))
                .OrderBy(membership => membership.UserId == venueCreatorUserId.Value
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

    private async Task<Guid?> GetVenueCreatorUserIdAsync(int applicationId, CancellationToken ct)
    {
        var opportunityId = await repository.GetOpportunityIdAsync(applicationId, ct);
        if (opportunityId is null)
            return null;

        var opportunity = await opportunityRepository.GetByIdAsync(opportunityId.Value, ct);
        if (opportunity is null)
            return null;

        var venue = await venueRepository.GetByIdAsync(opportunity.VenueId, ct);
        return venue?.UserId;
    }

    public Task AppliedAsync(ApplicationEntity application) =>
        NotifyVenueAsync(
            application,
            $"{currentUser.Email} has applied to your concert opportunity",
            MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(ApplicationEntity application) =>
        NotifyVenueAsync(
            application,
            $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application has been accepted!",
            MessageAction.ApplicationAccepted);

    public Task RejectedAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application was not selected for this concert opportunity",
            MessageAction.ApplicationRejected);

    public Task CancelledAsync(ApplicationEntity application) =>
        NotifyArtistAsync(
            application,
            "Your application was cancelled by the venue",
            MessageAction.ApplicationCancelled);

    private async Task NotifyVenueAsync(
        ApplicationEntity application,
        string content,
        MessageAction action)
    {
        await conversationsModule.SendAsync(
            application.VenueTenantId,
            application.ArtistTenantId,
            application.ArtistTenantId,
            currentUser.GetId(),
            content,
            action);
    }

    private async Task NotifyArtistAsync(
        ApplicationEntity application,
        string content,
        MessageAction action)
    {
        await conversationsModule.SendAndNotifyAsync(
            application.VenueTenantId,
            application.ArtistTenantId,
            application.VenueTenantId,
            currentUser.GetId(),
            content,
            action);
    }
}
