using Concertable.Auth.Contracts.Events;
using Concertable.Customer.Review.Contracts.Events;
using Concertable.Payment.Contracts.Events;

namespace Concertable.B2B.Hosting;

public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology
            .Subscribe<CustomerReviewSubmittedEvent>(B2BConstants.ServiceName)
            .Subscribe<CredentialRegisteredEvent>(B2BConstants.ServiceName)
            .Subscribe<PaymentSucceededEvent>(B2BConstants.ServiceName)
            .Subscribe<PaymentFailedEvent>(B2BConstants.ServiceName);
}
