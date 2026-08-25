using Concertable.B2B.DataAccess.Infrastructure.Extensions;
using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel.Identity;
using Concertable.Shared.Blob.Application;

namespace Concertable.B2B.Tenant.Infrastructure.Services;

internal sealed class VerificationService : IVerificationService
{
    private readonly IVerificationRepository repository;
    private readonly ITenantContext tenantContext;
    private readonly IBlobStorageService blobStorage;
    private readonly TimeProvider timeProvider;

    public VerificationService(
        IVerificationRepository repository,
        ITenantContext tenantContext,
        IBlobStorageService blobStorage,
        TimeProvider timeProvider)
    {
        this.repository = repository;
        this.tenantContext = tenantContext;
        this.blobStorage = blobStorage;
        this.timeProvider = timeProvider;
    }

    public async Task<Option<VerificationStatusDto>> GetOwnAsync(CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } tenantId)
            return Option.None<VerificationStatusDto>();

        return (await repository.GetByTenantIdAsync(tenantId, ct)).ToOption().Map(v => v.ToDto());
    }

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

    private async Task<IReadOnlyList<VerificationDocumentEntity>> UploadEvidenceAsync(
        Guid tenantId,
        IReadOnlyList<EvidenceUpload> uploads,
        DateTime now,
        CancellationToken ct)
    {
        var documents = new List<VerificationDocumentEntity>();

        foreach (var upload in uploads)
        {
            var blobName = VerificationDocumentEntity.BuildBlobName(tenantId, upload.DocumentType, upload.FileExtension);

            await using var stream = upload.Content;
            await blobStorage.UploadAsync(stream, blobName);

            documents.Add(VerificationDocumentEntity.Create(upload.DocumentType, blobName, now));
        }

        return documents;
    }
}
