using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Application.Contracts.Commands;

[MessageType("concertable.b2b.notify-application-payment-verification-failed.v1")]
public sealed record NotifyPaymentVerificationFailedCommand(
    int ApplicationId,
    string FailureMessage) : IIntegrationCommand;
