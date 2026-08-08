using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IPaidAcceptStep : IConcertStep
{
    Task ExecuteAsync(ApplicationEntity application, string paymentMethodId);
}
