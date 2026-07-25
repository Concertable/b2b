using Concertable.Auth.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;

public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology.ForService(AppHostConstants.ServiceNames.B2B)
            .Subscribe<CustomerReviewSubmittedEvent>()
            .Subscribe<CredentialRegisteredEvent>()
            .Subscribe<PaymentSucceededEvent>()
            .Subscribe<PaymentFailedEvent>()
            .Topology;
}
