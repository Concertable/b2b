using Concertable.B2B.Tenant.Application.Requests;
using Concertable.B2B.Tenant.Domain.Enums;
using Concertable.Kernel.Identity;
using Concertable.Shared.Blob.Application;
using Microsoft.AspNetCore.Http;

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
        SubmitVerificationRequest request,
        CancellationToken ct = default)
    {
        var tenantId = tenantContext.GetTenantId();
        var existing = await repository.GetByTenantIdAsync(tenantId, ct);
        if (existing is not null && existing.Status != TenantVerificationStatus.Rejected)
            return new SubmitVerificationError.NotEligible(existing.Status);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var documents = await UploadEvidenceAsync(tenantId, request, ct);

        if (existing is null)
        {
            var verification = TenantVerificationEntity.Submit(tenantId, documents, now);
            await repository.InsertAsync(verification, ct);
            return verification.ToDto();
        }

        existing.Resubmit(documents, now);
        await repository.SaveChangesAsync(ct);
        return existing.ToDto();
    }

    private async Task<List<VerificationDocumentEntity>> UploadEvidenceAsync(
        Guid tenantId,
        SubmitVerificationRequest request,
        CancellationToken ct)
    {
        var documents = new List<VerificationDocumentEntity>();
        var now = timeProvider.GetUtcNow().UtcDateTime;

        for (var i = 0; i < request.Files.Count; i++)
        {
            IFormFile file = request.Files[i];
            var documentType = request.DocumentTypes[i];
            var blobName = $"verification-evidence/{tenantId}-{documentType}-{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

            await using var stream = file.OpenReadStream();
            await blobStorage.UploadAsync(stream, blobName);

            documents.Add(VerificationDocumentEntity.Create(documentType, blobName, now));
        }

        return documents;
    }
}
