using Concertable.Payment.Application.Interfaces;
using Concertable.Payment.Application.Interfaces.Webhook;
using Concertable.Payment.Application.Requests;
using Concertable.Payment.Application.DTOs;
using FluentResults;
using Stripe;

namespace Concertable.B2B.IntegrationTests.Fixtures.Mocks;

internal class MockStripePaymentIntentClient : IStripePaymentIntentClient
{
    private readonly IStripeApiClient stripeApiClient;

    public MockStripePaymentIntentClient(IStripeApiClient stripeApiClient)
    {
        this.stripeApiClient = stripeApiClient;
    }

    public async Task<Result<PaymentResponse>> ChargeAsync(StripeChargeOptions opts)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(opts.Amount * 100),
            Metadata = opts.Metadata
        });
        return ToResult(intent);
    }

    public async Task<Result<PaymentResponse>> HoldAsync(StripeHoldOptions opts)
    {
        var intent = await stripeApiClient.CreatePaymentIntentAsync(new PaymentIntentCreateOptions
        {
            Amount = (long)(opts.Amount * 100),
            Metadata = opts.Metadata
        });
        return ToResult(intent);
    }

    private static Result<PaymentResponse> ToResult(PaymentIntent intent) =>
        intent.Status is not ("succeeded" or "requires_action" or "requires_confirmation")
            ? Result.Fail($"Payment failed with status: {intent.Status}")
            : Result.Ok(new PaymentResponse
            {
                RequiresAction = intent.Status is "requires_action" or "requires_confirmation",
                ClientSecret = intent.ClientSecret,
                TransactionId = intent.Id
            });
}
