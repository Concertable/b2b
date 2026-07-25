using Concertable.Auth.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;

public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology
            .Subscribe<CustomerReviewSubmittedEvent>("b2b-review-submitted",     "concertable-b2b")
            .Subscribe<CredentialRegisteredEvent>("b2b-credential-registered", "concertable-b2b")
            .Subscribe<PaymentSucceededEvent>("b2b-payment-succeeded",        "concertable-b2b")
            .Subscribe<PaymentFailedEvent>("b2b-payment-failed",           "concertable-b2b");
}
