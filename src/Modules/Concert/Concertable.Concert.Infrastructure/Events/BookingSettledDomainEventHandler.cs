using Concertable.Concert.Domain.Events;
using Concertable.Shared.Exceptions;

namespace Concertable.Concert.Infrastructure.Events;

internal class BookingSettledDomainEventHandler : IDomainEventHandler<BookingSettledDomainEvent>
{
    private readonly IConcertDraftService concertDraftService;

    public BookingSettledDomainEventHandler(IConcertDraftService concertDraftService)
    {
        this.concertDraftService = concertDraftService;
    }

    public async Task HandleAsync(BookingSettledDomainEvent e, CancellationToken ct = default)
    {
        if (e.ContractType is ContractType.DoorSplit or ContractType.Versus)
            return;

        var result = await concertDraftService.CreateAsync(e.BookingId);
        if (result.IsFailed)
            throw new BadRequestException(result.Errors);
    }
}
