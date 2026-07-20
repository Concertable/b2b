using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Tenant.Contracts;

namespace Concertable.B2B.Concert.Infrastructure.Services;

internal sealed class InvoiceIssuer : IInvoiceIssuer
{
    private readonly ISettlementAmountResolver settlementAmountResolver;
    private readonly ISettlementPayeeResolver settlementPayeeResolver;
    private readonly ITicketPayeeResolver ticketPayeeResolver;
    private readonly IDealAccessor dealAccessor;
    private readonly ITenantModule tenantModule;
    private readonly IInvoiceRepository invoiceRepository;
    private readonly ISequenceRepository<InvoiceSequenceEntity> sequenceRepository;
    private readonly TimeProvider timeProvider;

    public InvoiceIssuer(
        ISettlementAmountResolver settlementAmountResolver,
        ISettlementPayeeResolver settlementPayeeResolver,
        ITicketPayeeResolver ticketPayeeResolver,
        IDealAccessor dealAccessor,
        ITenantModule tenantModule,
        IInvoiceRepository invoiceRepository,
        ISequenceRepository<InvoiceSequenceEntity> sequenceRepository,
        TimeProvider timeProvider)
    {
        this.settlementAmountResolver = settlementAmountResolver;
        this.settlementPayeeResolver = settlementPayeeResolver;
        this.ticketPayeeResolver = ticketPayeeResolver;
        this.dealAccessor = dealAccessor;
        this.tenantModule = tenantModule;
        this.invoiceRepository = invoiceRepository;
        this.sequenceRepository = sequenceRepository;
        this.timeProvider = timeProvider;
    }

    public async Task IssueAsync(ConcertEntity concert, CancellationToken ct = default)
    {
        var gross = await settlementAmountResolver.ResolveGrossAsync(concert.Id, dealAccessor.Deal, ct);

        var supplierTenantId = settlementPayeeResolver.ResolveTenantId(concert);
        var customerTenantId = ticketPayeeResolver.ResolveTenantId(concert);

        var supplierTax = await tenantModule.GetTaxComplianceAsync(supplierTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Supplier tenant {supplierTenantId} has no tax compliance at invoice time; the settlement tax-gate should guarantee it.");
        var customerTax = await tenantModule.GetTaxComplianceAsync(customerTenantId, ct)
            ?? throw new InvalidOperationException(
                $"Customer tenant {customerTenantId} has no tax compliance at invoice time; the settlement tax-gate should guarantee it.");

        var supplier = await BuildPartyAsync(supplierTenantId, supplierTax, ct);
        var customer = await BuildPartyAsync(customerTenantId, customerTax, ct);

        var vat = await tenantModule.GetVatCalculationAsync(supplierTenantId, gross, ct);

        var sequenceNumber = await sequenceRepository.AllocateNextAsync(supplierTenantId, ct);
        var invoiceNumber = $"INV-{supplierTax.SellerIdentifier}-{sequenceNumber:D6}";

        var invoice = InvoiceEntity.Create(
            concert.BookingId,
            supplier,
            customer,
            new VatBreakdown(vat.Net, vat.Vat, gross, vat.Rate),
            sequenceNumber,
            invoiceNumber,
            concert.Period.End,
            concert.DealType,
            timeProvider.GetUtcNow().UtcDateTime);
        invoice.VenueTenantId = concert.VenueTenantId;
        invoice.ArtistTenantId = concert.ArtistTenantId;

        await invoiceRepository.AddAsync(invoice, ct);
    }

    private async Task<InvoiceParty> BuildPartyAsync(Guid tenantId, TaxComplianceDto tax, CancellationToken ct)
    {
        var tenant = await tenantModule.GetByIdAsync(tenantId, ct)
            ?? throw new InvalidOperationException($"Tenant {tenantId} not found at invoice time.");
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
