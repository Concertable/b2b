using Concertable.Payment.Client;
using Concertable.Payment.Domain;
using Concertable.Payment.Infrastructure.Data;
using FluentResults;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockEscrowClient : IEscrowClient
{
    private readonly MockStripeApiClient stripeApiClient;
    private readonly PaymentDbContext dbContext;

    public MockEscrowClient(MockStripeApiClient stripeApiClient, PaymentDbContext dbContext)
    {
        this.stripeApiClient = stripeApiClient;
        this.dbContext = dbContext;
    }

    public async Task<Result<EscrowResponse>> DepositAsync(Guid payerId, Guid payeeId, decimal amount, string paymentMethodId, PaymentSession session, int bookingId, CancellationToken ct = default)
    {
        var metadata = new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Escrow,
            ["bookingId"] = bookingId.ToString()
        };
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(amount * 100),
            Metadata = metadata
        });

        var escrow = EscrowEntity.Create(bookingId, payerId, payeeId, (long)(amount * 100), intent.Id);
        escrow.Confirm();
        dbContext.Escrows.Add(escrow);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(new EscrowResponse(escrow.Id, escrow.ChargeId, escrow.Status));
    }

    public async Task<Result<EscrowResponse>> CaptureAsync(Guid payerId, Guid payeeId, decimal amount, string paymentIntentId, int bookingId, CancellationToken ct = default)
    {
        stripeApiClient.UpdateLastMetadata(new Dictionary<string, string>
        {
            ["type"] = TransactionTypes.Escrow,
            ["bookingId"] = bookingId.ToString()
        });

        var escrow = EscrowEntity.Create(bookingId, payerId, payeeId, (long)(amount * 100), paymentIntentId);
        escrow.Confirm();
        dbContext.Escrows.Add(escrow);
        await dbContext.SaveChangesAsync(ct);

        return Result.Ok(new EscrowResponse(escrow.Id, escrow.ChargeId, escrow.Status));
    }

    public Task<Result<TransferResponse?>> ReleaseByBookingIdAsync(int bookingId, CancellationToken ct = default) =>
        Task.FromResult(Result.Ok<TransferResponse?>(new TransferResponse("tr_mock")));
}
