using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.DataAccess.Infrastructure.Extensions;
using Concertable.Kernel.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow;

internal sealed class BookingAdvancer : IBookingAdvancer
{
    private readonly IApplicationRepository applicationRepository;
    private readonly IVerifyExecutor verifyExecutor;

    public BookingAdvancer(
        IApplicationRepository applicationRepository,
        IVerifyExecutor verifyExecutor)
    {
        this.applicationRepository = applicationRepository;
        this.verifyExecutor = verifyExecutor;
    }

    public async Task AdvanceIfReadyAsync(int applicationId, CancellationToken ct = default)
    {
        var state = await applicationRepository.GetLifecycleAndPaymentStateAsync(applicationId, ct);
        if (state is not { } s || !IsBookingPending(s.State))
            return;

        try
        {
            await (s.Verification switch
            {
                PaymentVerification.Verified => verifyExecutor.VerifiedAsync(applicationId, ct),
                PaymentVerification.Failed => verifyExecutor.FailedAsync(applicationId, ct),
                _ => Task.CompletedTask,
            });
        }
        catch (ConflictException) { }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey()) { }
    }

    private static bool IsBookingPending(LifecycleState state)
        => state is LifecycleState.Accepted or LifecycleState.PaymentFailed;
}
