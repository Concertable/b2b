using Concertable.B2B.Artist.Contracts;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.B2B.Venue.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class VerificationAdminService : IVerificationAdminService
{
    private readonly IVerificationRepository repository;
    private readonly ITenantRepository tenantRepository;
    private readonly IVenueModule venueModule;
    private readonly IArtistModule artistModule;
    private readonly IVerificationNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<VerificationAdminService> logger;

    public VerificationAdminService(
        IVerificationRepository repository,
        ITenantRepository tenantRepository,
        IVenueModule venueModule,
        IArtistModule artistModule,
        IVerificationNotifier notifier,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        ILogger<VerificationAdminService> logger)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.venueModule = venueModule;
        this.artistModule = artistModule;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<IPagination<PendingVerificationDto>> GetPendingAsync(
        IPageParams pageParams,
        CancellationToken ct = default)
    {
        var pending = await repository.GetPendingAsync(pageParams);

        // Sequential, not Task.WhenAll: two pending rows of the same TenantType would otherwise run
        // concurrent queries against the same scoped Venue/ArtistReadDbContext instance, which EF Core
        // forbids ("a second operation was started on this context before a previous operation completed").
        var rows = new List<PendingVerificationDto>(pending.Data.Count);
        foreach (var row in pending.Data)
            rows.Add(await ToDtoAsync(row, ct));

        return new Pagination<PendingVerificationDto>(rows, pending.TotalCount, pending.PageNumber, pending.PageSize);
    }

    public Task<UnitResult<VerificationReviewError>> ApproveAsync(Guid tenantId, CancellationToken ct = default) =>
        ReviewAsync(
            tenantId,
            verification => verification.Approve(currentUser.GetId(), timeProvider.GetUtcNow().UtcDateTime),
            notifier.NotifyApprovedAsync,
            ct);

    public Task<UnitResult<VerificationReviewError>> RejectAsync(
        Guid tenantId,
        string reason,
        CancellationToken ct = default) =>
        ReviewAsync(
            tenantId,
            verification => verification.Reject(currentUser.GetId(), reason, timeProvider.GetUtcNow().UtcDateTime),
            notifier.NotifyRejectedAsync,
            ct);

    private async Task<UnitResult<VerificationReviewError>> ReviewAsync(
        Guid tenantId,
        Action<TenantVerificationEntity> transition,
        Func<TenantVerificationEntity, string?, Task> notify,
        CancellationToken ct)
    {
        var verification = await repository.GetByTenantIdAsync(tenantId, ct);
        if (verification is null)
            return new VerificationReviewError.NotFound(tenantId);
        if (verification.Status != TenantVerificationStatus.Pending)
            return new VerificationReviewError.NotPending(verification.Status);

        transition(verification);
        await repository.SaveChangesAsync(ct);

        try
        {
            var tenant = await tenantRepository.GetByIdAsync(tenantId, ct);
            var contactEmail = tenant is null ? null : (await GetContactAsync(tenant.Type, tenantId, ct)).Email;
            await notify(verification, contactEmail);
        }
        catch (Exception exception)
        {
            // The persisted review decision is the record the admin action turns on; a notification
            // failure must not fail a request whose write already committed, or a retry just hits
            // VerificationReviewError.NotPending against the decision that already landed.
            logger.VerificationReviewNotificationFailed(tenantId, exception);
        }

        return new Success();
    }

    private async Task<PendingVerificationDto> ToDtoAsync(PendingVerificationProjection pending, CancellationToken ct)
    {
        var contact = await GetContactAsync(pending.TenantType, pending.TenantId, ct);
        return new PendingVerificationDto
        {
            TenantId = pending.TenantId,
            TenantType = pending.TenantType,
            Name = contact.Name,
            Email = contact.Email,
            SubmittedAt = pending.SubmittedAt,
        };
    }

    private async Task<(string? Name, string? Email)> GetContactAsync(
        TenantType type,
        Guid tenantId,
        CancellationToken ct)
    {
        if (type == TenantType.Venue)
        {
            return (await venueModule.GetContactByTenantIdAsync(tenantId, ct)).TryGetValue(out var venueContact)
                ? (venueContact.Name, venueContact.Email)
                : (null, null);
        }

        return (await artistModule.GetContactByTenantIdAsync(tenantId, ct)).TryGetValue(out var artistContact)
            ? (artistContact.Name, artistContact.Email)
            : (null, null);
    }
}
