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

    public async Task<SelfBillingAgreementDto?> GetCurrentAsync(CancellationToken ct = default)
    {
        var agreement = await repository.GetCurrentAsync(timeProvider.GetUtcNow().UtcDateTime, ct);
        return agreement?.ToDto();
    }

    public async Task GrantAsync(ESignatureRequest eSignature, CancellationToken ct = default)
    {
        var supplierTenantId = tenantContext.TenantId
            ?? throw new ForbiddenException("No tenant for the current request.");

        var tenant = await tenantModule.GetByIdAsync(supplierTenantId, ct)
            ?? throw new NotFoundException($"Tenant {supplierTenantId} not found.");
        var tax = await tenantModule.GetTaxComplianceAsync(supplierTenantId, ct)
            ?? throw new BadRequestException("Complete your tax details before granting a self-billing agreement.");

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
            RenderClause(tenant.LegalName),
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

    private static string RenderClause(string supplierLegalName) =>
        $"""
        This self-billing agreement is made between Concertable (the customer) and {supplierLegalName} (the supplier).

        The supplier agrees:
        - to accept invoices raised by the customer on the supplier's behalf until the expiry of this agreement;
        - not to raise its own VAT invoices for the supplies covered by this agreement;
        - to notify the customer immediately if it changes its VAT registration number, ceases to be VAT registered, or transfers its business as a going concern.

        The customer agrees:
        - to raise self-billed invoices for all supplies made by the supplier under this agreement until it expires;
        - to state on each invoice that it is a self-billed invoice raised on the supplier's behalf;
        - to make a new self-billing agreement with the supplier if the VAT registration number changes.

        This agreement is reviewed at least every 12 months and self-billed invoices are only raised while it is in force.
        """;
}
