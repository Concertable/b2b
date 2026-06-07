namespace Concertable.B2B.Concert.Application.Workflow.Steps;

internal interface IBookStep : IConcertStep
{
    Task ExecuteAsync(int bookingId);
}
