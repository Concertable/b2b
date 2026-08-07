using Concertable.B2B.Concert.Application.Requests;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.ValueObjects;
using Concertable.B2B.Concert.Infrastructure.Pdf;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;
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

    public async Task GrantAsync(ESignatureRequest eSignature, CancellationToken ct = default)
    {
        var supplierTenantId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for the current request.");

        var tenant = (await tenantModule.GetByIdAsync(supplierTenantId, ct)).Match(
            value => value,
            () => throw new NotFoundException($"Tenant {supplierTenantId} not found."));
        var tax = (await tenantModule.GetTaxComplianceAsync(supplierTenantId, ct)).Match(
            value => value,
            () => throw new BadRequestException("Complete your tax details before granting a self-billing agreement."));

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
            currentUser.Id ?? throw new ForbiddenException("No user for current request"),
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
    }

    public async Task<FileDownload> GetPdfAsync(CancellationToken ct = default)
    {
        var agreement = await repository.GetCurrentAsync(timeProvider.GetUtcNow().UtcDateTime, ct)
            .OrNotFound(DisplayNames.SelfBillingAgreement);
        var blobName = agreement.PdfBlobName
            ?? throw new InvalidOperationException("Self-billing agreement has no assigned PDF blob name");
        var bytes = await pdfCache.GetOrCreateAsync(blobName, new SelfBillingAgreementDocument(agreement, logger), ct);
        return agreement.ToFileDownload(bytes);
    }
}
