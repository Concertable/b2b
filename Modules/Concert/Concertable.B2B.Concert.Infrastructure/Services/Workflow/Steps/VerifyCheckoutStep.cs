using Concertable.B2B.Concert.Application.Mappers;
using Concertable.B2B.Concert.Application.Responses;
using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Contract.Contracts;
using Concertable.B2B.Tenant.Contracts;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class VerifyCheckoutStep : IAcceptCheckoutStep
{
    private readonly IPayerLookup payerLookup;
    private readonly IContractAccessor contractAccessor;
    private readonly IManagerPaymentClient managerPaymentClient;
    private readonly IPaymentAmountMapper paymentAmountMapper;
    private readonly ITenantModule tenantModule;

    public VerifyCheckoutStep(
        IPayerLookup payerLookup,
        IContractAccessor contractAccessor,
        IManagerPaymentClient managerPaymentClient,
        IPaymentAmountMapper paymentAmountMapper,
        ITenantModule tenantModule)
    {
        this.payerLookup = payerLookup;
        this.contractAccessor = contractAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.paymentAmountMapper = paymentAmountMapper;
        this.tenantModule = tenantModule;
    }

    public async Task<Checkout> ExecuteAsync(int applicationId)
    {
        var artist = await payerLookup.GetArtistAsync(applicationId)
            ?? throw new NotFoundException("Application not found");
        var venueManagerId = await payerLookup.GetVenueManagerIdAsync(applicationId)
            ?? throw new NotFoundException("Application not found");

        var metadata = new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Verify,
            ["applicationId"] = applicationId.ToString(),
            ["venueManagerId"] = venueManagerId.ToString()
        };

        var venueOwnerId = await tenantModule.GetTenantIdByUserIdAsync(venueManagerId)
            ?? throw new NotFoundException($"No tenant for user {venueManagerId}");

        var session = await managerPaymentClient.CreateVerifySessionAsync(venueOwnerId, metadata);
        var amount = paymentAmountMapper.ToPaymentAmount(contractAccessor.Contract);
        return new Checkout(amount, artist, session, CheckoutLabels.Settlement);
    }
}
