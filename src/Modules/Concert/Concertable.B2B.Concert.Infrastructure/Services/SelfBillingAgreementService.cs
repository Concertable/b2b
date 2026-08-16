using Concertable.B2B.Concert.Application.Errors;
using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class SelfBillingAgreementService : ISelfBillingAgreementService
{
    private static readonly TimeSpan RenewalWindow = TimeSpan.FromDays(30);
    private readonly ISelfBillingAgreementRepository repository;
    private readonly ITenantModule tenantModule;
    private readonly ICurrentUser currentUser;
    private readonly IClientContext clientContext;
    private readonly IPdfBlobCache pdfCache;
    private readonly ITenantContext tenantContext;
    private readonly LegalSettings legal;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<SelfBillingAgreementService> logger;

    public SelfBillingAgreementService(
        ISelfBillingAgreementRepository repository,
        ITenantModule tenantModule,
        ICurrentUser currentUser,
        IClientContext clientContext,
        IPdfBlobCache pdfCache,
        ITenantContext tenantContext,
        IOptions<LegalSettings> legal,
        TimeProvider timeProvider,
        ILogger<SelfBillingAgreementService> logger)
    {
        this.repository = repository;
        this.tenantModule = tenantModule;
        this.currentUser = currentUser;
        this.clientContext = clientContext;
        this.pdfCache = pdfCache;
        this.tenantContext = tenantContext;
        this.legal = legal.Value;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<SelfBillingAgreementStatusDto> GetStatusAsync(CancellationToken ct = default)
    {
        var agreement = await repository.GetLatestAsync(ct);
        if (agreement is null)
            return new SelfBillingAgreementStatusDto(null, IsInForce: false, CanRenew: false);

        var dto = agreement.ToDto();
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;
        var isInForce = dto.ExpiresAtUtc > utcNow;
        var canRenew = !isInForce || dto.ExpiresAtUtc - utcNow <= RenewalWindow;
        return new SelfBillingAgreementStatusDto(dto, isInForce, canRenew);
    }

    public async Task<UnitResult<GrantSelfBillingAgreementError>> GrantAsync(
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        if (tenantContext.TenantId is not { } supplierTenantId)
            return new GrantSelfBillingAgreementError.MissingTenant();

        var tenantOption = await tenantModule.GetByIdAsync(supplierTenantId, ct);
        if (!tenantOption.TryGetValue(out var tenant))
            return new GrantSelfBillingAgreementError.TenantNotFound(supplierTenantId);

        var taxOption = await tenantModule.GetTaxComplianceAsync(supplierTenantId, ct);
        if (!taxOption.TryGetValue(out var tax))
            return new GrantSelfBillingAgreementError.MissingTaxCompliance();

        if (currentUser.Id is not { } userId)
            return new GrantSelfBillingAgreementError.MissingUser();

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var address = tax.RegisteredAddress;
        var supplier = new InvoiceParty(
            supplierTenantId,
            tenant.LegalName,
            tax.VatNumber,
            address.Line1,
            address.Line2,
            address.City,
            address.Postcode,
            address.Country);

        var signature = new ESignature(
            userId,
            now,
            clientContext.IpAddress,
            clientContext.UserAgent,
            eSignature.SignatoryName,
            eSignature.DrawnSignatureImage);

        var agreement = SelfBillingAgreementEntity.Create(
            supplierTenantId,
            supplier,
            signature,
            SelfBillingClause.Render(tenant.LegalName),
            legal.PlatformTermsVersion,
            now,
            now);

        await repository.AddAsync(agreement, ct);
        await repository.SaveChangesAsync(ct);
        return new Success();
    }

    public async Task<Result<FileDownload, SelfBillingAgreementPdfError>> GetPdfAsync(
        CancellationToken ct = default) =>
        await repository.GetCurrentAsync(timeProvider.GetUtcNow().UtcDateTime, ct)
            .ToOption()
            .OrFailure(() => (SelfBillingAgreementPdfError)new SelfBillingAgreementPdfError.NotFound())
            .MapAsync(async agreement =>
            {
                var blobName = agreement.PdfBlobName
                    ?? throw new InvalidOperationException("Self-billing agreement has no assigned PDF blob name");
                var bytes = await pdfCache.GetOrCreateAsync(
                    blobName,
                    new SelfBillingAgreementDocument(agreement, logger),
                    ct);
                return agreement.ToFileDownload(bytes);
            });
}
