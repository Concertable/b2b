using Concertable.Concert.Domain.Events;
using Concertable.Shared;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Events;

internal class ApplicationAcceptedDomainEventHandler : IDomainEventHandler<ApplicationAcceptedDomainEvent>
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IBookingRepository bookingRepository;
    private readonly IBackgroundTaskRunner taskRunner;

    public ApplicationAcceptedDomainEventHandler(
        IApplicationRepository applicationRepository,
        IBookingRepository bookingRepository,
        IBackgroundTaskRunner taskRunner)
    {
        this.applicationRepository = applicationRepository;
        this.bookingRepository = bookingRepository;
        this.taskRunner = taskRunner;
    }

    public async Task HandleAsync(ApplicationAcceptedDomainEvent e, CancellationToken ct = default)
    {
        var application = await applicationRepository.GetByIdAsync(e.ApplicationId)
            ?? throw new NotFoundException("Application not found");
        var booking = await bookingRepository.GetByApplicationIdAsync(e.ApplicationId)
            ?? throw new NotFoundException("Booking not found for application");

        application.Accept(booking);
        await applicationRepository.SaveChangesAsync();

        await taskRunner.RunAsync<IApplicationRepository>(
            (repo, runCt) => repo.RejectAllExceptAsync(e.OpportunityId, e.ApplicationId));
    }
}
