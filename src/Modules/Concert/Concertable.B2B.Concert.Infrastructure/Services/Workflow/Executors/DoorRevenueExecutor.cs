using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.Kernel.Exceptions;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class DoorRevenueExecutor : IDoorRevenueExecutor
{
    private readonly IConcertRepository concertRepository;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DoorRevenueExecutor> logger;

    public DoorRevenueExecutor(
        IConcertRepository concertRepository,
        TimeProvider timeProvider,
        ILogger<DoorRevenueExecutor> logger)
    {
        this.concertRepository = concertRepository;
        this.timeProvider = timeProvider;
        this.logger = logger;
    }

    public async Task<Result> ExecuteAsync(int concertId, decimal doorRevenue)
    {
        try
        {
            var concert = await concertRepository.GetByIdWithBookingAsync(concertId)
                .OrNotFound();

            /* Only revenue-share settlements (DeferredBooking) take a declared door figure. */
            if (concert.Booking is not DeferredBooking)
                throw new BadRequestException("Door revenue can only be declared for a revenue-share concert.");
            if (timeProvider.GetUtcNow().UtcDateTime < concert.Period.End)
                throw new BadRequestException("Door revenue can only be declared after the concert has ended.");
            /* Re-declarable while still Booked; frozen once the settlement sweep has moved it on. */
            if (concert.Booking.Application.State != LifecycleState.Booked)
                throw new ConflictException("Door revenue can only be declared before the concert has settled.");

            concert.DeclareDoorRevenue(doorRevenue);
            await concertRepository.SaveChangesAsync();

            logger.DoorRevenueDeclared(concertId, doorRevenue);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            logger.FailedToDeclareDoorRevenue(concertId, ex);
            return Result.Fail(ex.Message);
        }
    }
}
