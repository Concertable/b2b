using Concertable.B2B.Application.Contracts.Commands;
using Concertable.B2B.Application.Infrastructure.Services;
using Concertable.Messaging.Contracts;

namespace Concertable.B2B.Application.Infrastructure.Handlers;

internal sealed class NotifyApplicationPaymentVerificationFailedCommandHandler(
    IApplicationNotifier notifier)
    : IIntegrationCommandHandler<NotifyApplicationPaymentVerificationFailedCommand>
{
    public Task HandleAsync(
        NotifyApplicationPaymentVerificationFailedCommand command,
        MessageEnvelope envelope,
        CancellationToken ct = default) =>
        notifier.VerifyPaymentFailedAsync(command.ApplicationId, command.FailureMessage);
}
