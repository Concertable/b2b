using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceIssuer : IInvoiceIssuer
{
    private readonly ITenantModule tenantModule;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly ISequenceRepository<InvoiceSequenceEntity> sequenceRepository;
    private readonly TimeProvider timeProvider;

    public InvoiceIssuer(
        ITenantModule tenantModule,
        IInvoiceRepository invoiceRepository,
        ISequenceRepository<InvoiceSequenceEntity> sequenceRepository,
        TimeProvider timeProvider)
    {
        this.tenantModule = tenantModule;
        this.invoiceRepository = invoiceRepository;
        this.sequenceRepository = sequenceRepository;
        this.timeProvider = timeProvider;
    }

    public async Task IssueAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        var gross = Money.Gbp(concert.CalculateSettlementGross());

        var supplierTenantId = concert.SettlementPayeeTenantId;
        var customerTenantId = concert.SettlementPayerTenantId;

        var supplierTax = (await tenantModule.GetTaxComplianceAsync(supplierTenantId, ct)).Match(
            value => value,
            () => throw new InvalidOperationException(
                $"Supplier tenant {supplierTenantId} has no tax compliance at invoice time; the settlement tax-gate should guarantee it."));
        var customerTax = (await tenantModule.GetTaxComplianceAsync(customerTenantId, ct)).Match(
            value => value,
            () => throw new InvalidOperationException(
                $"Customer tenant {customerTenantId} has no tax compliance at invoice time; the settlement tax-gate should guarantee it."));

        var supplier = await BuildPartyAsync(supplierTenantId, supplierTax, ct);
        var customer = await BuildPartyAsync(customerTenantId, customerTax, ct);

        var vat = (await tenantModule.GetVatCalculationAsync(supplierTenantId, gross.Amount, ct)).Match(
            value => value,
            _ => throw new InvalidOperationException($"Supplier tenant {supplierTenantId} not found at invoice time."));

        var sequenceNumber = await sequenceRepository.AllocateNextAsync(supplierTenantId, ct);
        var invoiceNumber = $"INV-{supplierTax.SellerIdentifier}-{sequenceNumber:D6}";

        var invoice = InvoiceEntity.Create(
            concert,
            supplier,
            customer,
            new VatBreakdown(vat.Net, vat.Vat, gross.Amount, vat.Rate),
            sequenceNumber,
            invoiceNumber,
            concert.Period.End,
            timeProvider.GetUtcNow().UtcDateTime);

        await invoiceRepository.AddAsync(invoice, ct);
    }

    private async Task<InvoiceParty> BuildPartyAsync(Guid tenantId, TaxComplianceDto tax, CancellationToken ct)
    {
        var tenant = (await tenantModule.GetByIdAsync(tenantId, ct)).Match(
            value => value,
            () => throw new InvalidOperationException($"Tenant {tenantId} not found at invoice time."));
        var address = tax.RegisteredAddress;
        return new InvoiceParty(
            tenantId,
            tenant.LegalName,
            tax.VatNumber,
            address.Line1,
            address.Line2,
            address.City,
            address.Postcode,
            address.Country);
    }
}
