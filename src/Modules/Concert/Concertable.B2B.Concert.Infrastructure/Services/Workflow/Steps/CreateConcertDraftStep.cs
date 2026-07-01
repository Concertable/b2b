using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CreateConcertDraftStep : IBookStep
{
    private readonly IConcertDraftService concertDraftService;

    public CreateConcertDraftStep(IConcertDraftService concertDraftService)
    {
        this.concertDraftService = concertDraftService;
    }

    public async Task ExecuteAsync(int bookingId)
    {
        var result = await concertDraftService.CreateAsync(bookingId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }
}
