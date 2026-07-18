using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;
using Concertable.Kernel.Exceptions;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class AcceptExecutor : IAcceptExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IBookingRepository bookingRepository;
    private readonly IContractIssuer contractIssuer;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IBackgroundTaskRunner taskRunner;

    public AcceptExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        IBookingRepository bookingRepository,
        IContractIssuer contractIssuer,
        ITermsFingerprintCalculator termsFingerprint,
        IBackgroundTaskRunner taskRunner)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.bookingRepository = bookingRepository;
        this.contractIssuer = contractIssuer;
        this.termsFingerprint = termsFingerprint;
        this.taskRunner = taskRunner;
    }

    public Task ExecuteAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature)
        => transitioner.TransitionAsync(applicationId, Trigger.Accept, async app =>
        {
            var deal = await dealResolver.ResolveByApplicationIdAsync(app.Id);
            VerifyTermsUnchanged(app, deal);

            var workflow = workflows.Create(app.DealType);
            await (workflow switch
            {
                IAcceptsPaid w when paymentMethodId is not null => w.Accept.ExecuteAsync(app.Id, paymentMethodId),
                IAcceptsPaid => throw new BadRequestException("This deal requires a payment method at acceptance"),
                IAcceptsSimple w => w.Accept.ExecuteAsync(app.Id),
                _ => throw new BadRequestException($"Deal {workflow.Type} does not support Accept")
            });

            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id)
                ?? throw new NotFoundException("Booking not found for application");
            app.Accept(booking);
            await contractIssuer.IssueAsync(app, booking.Id, eSignature);

            await taskRunner.RunAsync<IApplicationRepository>(
                (repo, runCt) => repo.RejectAllExceptAsync(app.OpportunityId, app.Id));
        });

    /* Must run BEFORE the accept step: the step captures/charges real money, and only the DB
       side of this transition rolls back on a throw. */
    private void VerifyTermsUnchanged(ApplicationEntity app, IDeal deal)
    {
        if (app.TermsFingerprint != termsFingerprint.Calculate(deal, app.Opportunity.Period))
            throw new ConflictException(
                "The deal terms have changed since the artist applied — the artist must re-apply before you can accept");
    }
}
