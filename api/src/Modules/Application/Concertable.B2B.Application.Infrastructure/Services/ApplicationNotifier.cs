using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Conversations.Contracts;
using Concertable.B2B.DataAccess.Infrastructure;
using Concertable.B2B.Authorization.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.B2B.Opportunity.Contracts;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Concertable.Kernel.Notifications;
using System.Security.Cryptography;
using System.Text;

namespace Concertable.B2B.Application.Infrastructure.Services;

internal sealed class ApplicationNotifier : IApplicationNotifier
{
    private static readonly Guid ConversationRequestNamespace =
        Guid.Parse("127b2cd6-b16f-53df-9853-158673bff66a");
    private readonly IApplicationPrivilegedRepository repository;
    private readonly ICurrentUser currentUser;
    private readonly IConversationsModule conversationsModule;
    private readonly INotificationClient notificationClient;
    private readonly IOpportunityPrivilegedReadRepository opportunityRepository;
    private readonly IVenuePrivilegedReadRepository venueRepository;
    private readonly ITenantModule tenantModule;
    private readonly ITransactionRunner transactionRunner;
    private readonly IPrivilegedOutboxUnitOfWorkBehavior unitOfWork;

    public ApplicationNotifier(
        IApplicationPrivilegedRepository repository,
        ICurrentUser currentUser,
        IConversationsModule conversationsModule,
        INotificationClient notificationClient,
        IOpportunityPrivilegedReadRepository opportunityRepository,
        IVenuePrivilegedReadRepository venueRepository,
        ITenantModule tenantModule,
        ITransactionRunner transactionRunner,
        IPrivilegedOutboxUnitOfWorkBehavior unitOfWork)
    {
        this.repository = repository;
        this.currentUser = currentUser;
        this.conversationsModule = conversationsModule;
        this.notificationClient = notificationClient;
        this.opportunityRepository = opportunityRepository;
        this.venueRepository = venueRepository;
        this.tenantModule = tenantModule;
        this.transactionRunner = transactionRunner;
        this.unitOfWork = unitOfWork;
    }

    public async Task VerifyPaymentFailedAsync(int applicationId, string failureMessage)
    {
        var venueTenantId = await repository.GetVenueTenantIdAsync(applicationId);
        if (venueTenantId is null)
            return;

        var venueCreatorUserId = await transactionRunner.RunAsync<ApplicationNotifier, Guid?>(
            (notifier, ct) => notifier.GetVenueCreatorUserIdAsync(applicationId, ct));
        if (venueCreatorUserId is null)
            return;

        await unitOfWork.ExecuteAsync(async () =>
        {
            var eligible = (await tenantModule.GetCurrentMembershipsForNotificationAsync(venueTenantId.Value))
                .Where(membership => membership.HasPermission(TenantPermission.ApplicationsDecide))
                .OrderBy(membership => membership.UserId == venueCreatorUserId.Value ? 0 : 1)
                .ThenBy(membership => membership.UserId);
            foreach (var candidate in eligible)
            {
                var recipientOption = await tenantModule.ResolveMembershipSnapshotAsync(candidate);
                if (!recipientOption.TryGetValue(out var recipient)
                    || !recipient.HasPermission(TenantPermission.ApplicationsDecide))
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
        NotifyAsync(
            application,
            $"{currentUser.Email} has applied to your concert opportunity",
            MessageAction.ApplicationReceived);

    public Task WithdrawnAsync(ApplicationEntity application) =>
        NotifyAsync(
            application,
            $"{currentUser.Email} has withdrawn their application to your concert opportunity",
            MessageAction.ApplicationWithdrawn);

    public Task AcceptedAsync(ApplicationEntity application) =>
        NotifyAsync(
            application,
            "Your application has been accepted!",
            MessageAction.ApplicationAccepted);

    public Task RejectedAsync(ApplicationEntity application) =>
        NotifyAsync(
            application,
            "Your application was not selected for this concert opportunity",
            MessageAction.ApplicationRejected);

    public Task CancelledAsync(ApplicationEntity application) =>
        NotifyAsync(
            application,
            "Your application was cancelled by the venue",
            MessageAction.ApplicationCancelled);

    private async Task NotifyAsync(
        ApplicationEntity application,
        string content,
        MessageAction action)
    {
        var conversationId = await conversationsModule.CreateAsync(
            RequestId(application.Id, "conversation"),
            [application.VenueTenantId, application.ArtistTenantId]);
        await conversationsModule.SendAsync(
            conversationId,
            RequestId(application.Id, action.ToString()),
            content,
            action);
    }

    private static Guid RequestId(int applicationId, string operation) =>
        new(MD5.HashData(Encoding.UTF8.GetBytes(
            $"{ConversationRequestNamespace:N}:{applicationId}:{operation}")));
}
