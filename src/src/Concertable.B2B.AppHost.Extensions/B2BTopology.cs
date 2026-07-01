public static class B2BTopology
{
    public static AsbTopology AddB2BTopology(this AsbTopology topology) =>
        topology
            .Subscribe("event-customerreviewsubmittedevent", "b2b-review-submitted",     "concertable-b2b")
            .Subscribe("event-credentialregisteredevent",    "b2b-credential-registered", "concertable-b2b")
            .Subscribe("event-paymentsucceededevent",        "b2b-payment-succeeded",     "concertable-b2b")
            .Subscribe("event-paymentfailedevent",           "b2b-payment-failed",        "concertable-b2b");
}
