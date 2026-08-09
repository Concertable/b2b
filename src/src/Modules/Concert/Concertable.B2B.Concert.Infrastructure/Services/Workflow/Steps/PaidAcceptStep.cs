using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class PaidAcceptStep : IPaidAcceptStep
{
    private readonly IBookingService bookingService;

    public PaidAcceptStep(IBookingService bookingService)
    {
        this.bookingService = bookingService;
    }

    public async Task<UnitResult<AcceptApplicationError>> ExecuteAsync(
        ApplicationEntity application,
        string paymentMethodId,
        CancellationToken ct = default)
    {
        await bookingService.CreateDeferredAsync(application, paymentMethodId);
        return UnitResult.Success<AcceptApplicationError>();
    }
}
