using Concertable.Auth.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;

public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology
            .Subscribe<CustomerReviewSubmittedEvent>(AppHostConstants.ServiceNames.B2B)
            .Subscribe<CredentialRegisteredEvent>(AppHostConstants.ServiceNames.B2B)
            .Subscribe<PaymentSucceededEvent>(AppHostConstants.ServiceNames.B2B)
            .Subscribe<PaymentFailedEvent>(AppHostConstants.ServiceNames.B2B);
}
