using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel.Identity;
using Concertable.Shared.Blob.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class VerificationService : IVerificationService
{
    private readonly IVerificationRepository repository;
    private readonly ITenantRepository tenantRepository;
    private readonly ITenantContext tenantContext;
    private readonly IBlobStorageService blobStorage;
    private readonly ITenantContactResolver contactResolver;
    private readonly IVerificationNotifier notifier;
    private readonly ICurrentUser currentUser;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<VerificationService> logger;

    public VerificationService(
        IVerificationRepository repository,
        ITenantRepository tenantRepository,
        ITenantContext tenantContext,
        IBlobStorageService blobStorage,
        ITenantContactResolver contactResolver,
        IVerificationNotifier notifier,
        ICurrentUser currentUser,
        TimeProvider timeProvider,
        ILogger<VerificationService> logger)
    {
        this.repository = repository;
        this.tenantRepository = tenantRepository;
        this.tenantContext = tenantContext;
        this.blobStorage = blobStorage;
        this.contactResolver = contactResolver;
        this.notifier = notifier;
        this.currentUser = currentUser;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Option<VerificationStatusDto>> GetStatusAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return null;

        return (await repository.GetByTenantIdAsync(tenantId, ct))?.ToDto();
    }

    public Task<bool> IsVerifiedAsync(Guid tenantId, CancellationToken ct = default) =>
        repository.IsApprovedByTenantIdAsync(tenantId, ct);

    public async Task<Result<VerificationStatusDto, SubmitVerificationError>> SubmitAsync(
        IReadOnlyList<EvidenceUpload> uploads,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var existing = await repository.GetByTenantIdAsync(tenantId, ct);
        if (existing is not null && existing.Status != TenantVerificationStatus.Rejected)
            return new SubmitVerificationError.NotEligible(existing.Status);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var documents = await UploadEvidenceAsync(tenantId, uploads, now, ct);

        if (existing is null)
        {
            var verification = TenantVerificationEntity.Submit(tenantId, documents, now);
            var inserted = await repository.TryInsertAsync(verification, ct);
            if (!inserted.TryGetValue(out var createdVerification))
            {
                // Lost the race against a concurrent first submission for this tenant — TenantId is
                // unique-indexed. Re-read the winner's status rather than assuming Pending.
                var current = await repository.GetByTenantIdAsync(tenantId, ct);
                return new SubmitVerificationError.NotEligible(current?.Status ?? TenantVerificationStatus.Pending);
            }
            return createdVerification.ToDto();
        }

        existing.Resubmit(documents, now);
        await repository.SaveChangesAsync(ct);
        return existing.ToDto();
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
        Func<TenantVerificationEntity, string, Task> notify,
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
            var resolved = tenant is null
                ? Option.None<TenantContact>()
                : await contactResolver.ResolveAsync(tenant.Type, tenantId, ct);

            if (resolved.TryGetValue(out var contact))
                await notify(verification, contact.Email);
            else
                logger.VerificationContactEmailMissing(tenantId);
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
        var contact = await contactResolver.ResolveAsync(pending.TenantType, pending.TenantId, ct);

        return new PendingVerificationDto
        {
            TenantId = pending.TenantId,
            TenantType = pending.TenantType,
            Contact = contact.ToNullable(),
            SubmittedAt = pending.SubmittedAt,
        };
    }

    private async Task<IReadOnlyList<VerificationDocumentEntity>> UploadEvidenceAsync(
        Guid tenantId,
        IReadOnlyList<EvidenceUpload> uploads,
        DateTime now,
        CancellationToken ct)
    {
        var documents = new List<VerificationDocumentEntity>();

        foreach (var upload in uploads)
        {
            var document = VerificationDocumentEntity.Create(tenantId, upload.DocumentType, upload.FileExtension, now);

            await using var stream = upload.Content;
            await blobStorage.UploadAsync(stream, document.BlobName);

            documents.Add(document);
        }

        return documents;
    }
}
