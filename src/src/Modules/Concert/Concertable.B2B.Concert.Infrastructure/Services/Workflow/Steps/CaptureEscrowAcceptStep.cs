using Concertable.B2B.Concert.Application.Workflow.Steps;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Deal.Contracts;
using Concertable.Kernel.Exceptions;
using Concertable.Kernel.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Steps;

internal sealed class CaptureEscrowAcceptStep : ISimpleAcceptStep
{
    private readonly IBookingService bookingService;
    private readonly IEscrowOperationsClient escrowClient;
    private readonly IDealAccessor dealAccessor;
    private readonly IManagerPaymentOperationsClient managerPaymentClient;
    private readonly ILogger<CaptureEscrowAcceptStep> logger;

    public CaptureEscrowAcceptStep(
        IBookingService bookingService,
        IEscrowOperationsClient escrowClient,
        IDealAccessor dealAccessor,
        IManagerPaymentOperationsClient managerPaymentClient,
        ILogger<CaptureEscrowAcceptStep> logger)
    {
        this.bookingService = bookingService;
        this.escrowClient = escrowClient;
        this.dealAccessor = dealAccessor;
        this.managerPaymentClient = managerPaymentClient;
        this.logger = logger;
    }

    public async Task ExecuteAsync(ApplicationEntity application)
    {
        var deal = (FlatFeeDeal)dealAccessor.Deal;
        var booking = await bookingService.CreateStandardAsync(application);

        var paymentIntentId = await managerPaymentClient.FindHeldIntentAsync(application.VenueTenantId, application.Id);

        logger.AcceptingFlatFeeApplication(
            application.Id,
            booking.Id,
            paymentIntentId,
            deal.Fee,
            "GBP",
            application.VenueTenantId,
            application.ArtistTenantId);

        var bind = await escrowClient.CaptureAsync(
            application.VenueTenantId,
            application.ArtistTenantId,
            Money.Gbp(deal.Fee),
            paymentIntentId,
            booking.Id);
        if (bind.TryGetError(out var error))
            throw new BadRequestException(error.Definition.Message);
    }
}
