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
    private readonly IContractResolver contractResolver;
    private readonly IBookingRepository bookingRepository;
    private readonly IBookingAgreementBuilder agreementBuilder;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IBackgroundTaskRunner taskRunner;

    public AcceptExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IContractResolver contractResolver,
        IBookingRepository bookingRepository,
        IBookingAgreementBuilder agreementBuilder,
        ITermsFingerprintCalculator termsFingerprint,
        IBackgroundTaskRunner taskRunner)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.contractResolver = contractResolver;
        this.bookingRepository = bookingRepository;
        this.agreementBuilder = agreementBuilder;
        this.termsFingerprint = termsFingerprint;
        this.taskRunner = taskRunner;
    }

    public Task ExecuteAsync(int applicationId, string? paymentMethodId, ESignatureRequest eSignature)
        => transitioner.TransitionAsync(applicationId, Trigger.Accept, async app =>
        {
            var contract = await contractResolver.ResolveByApplicationIdAsync(app.Id);
            VerifyTermsUnchanged(app, contract);

            var workflow = workflows.Create(app.ContractType);
            await (workflow switch
            {
                IAcceptsPaid w when paymentMethodId is not null => w.Accept.ExecuteAsync(app.Id, paymentMethodId),
                IAcceptsPaid => throw new BadRequestException("This contract requires a payment method at acceptance"),
                IAcceptsSimple w => w.Accept.ExecuteAsync(app.Id),
                _ => throw new BadRequestException($"Contract {workflow.Type} does not support Accept")
            });

            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id)
                ?? throw new NotFoundException("Booking not found for application");
            app.Accept(booking);
            await agreementBuilder.BuildAsync(app, booking.Id, eSignature);

            await taskRunner.RunAsync<IApplicationRepository>(
                (repo, runCt) => repo.RejectAllExceptAsync(app.OpportunityId, app.Id));

            /* Render + store the agreement PDF off the request thread once the transition commits;
               the download endpoint lazily regenerates if the blob is ever missing, so a blob
               outage here is non-fatal. */
            await taskRunner.RunAsync<IBookingAgreementPdfService>(
                (pdf, runCt) => pdf.GenerateForBookingAsync(booking.Id, runCt));
        });

    /* Must run BEFORE the accept step: the step captures/charges real money, and only the DB
       side of this transition rolls back on a throw. */
    private void VerifyTermsUnchanged(ApplicationEntity app, IContract contract)
    {
        /* Null fingerprint = the application predates click-wrap; there is no artist consent to
           protect, so the guard only applies once one was recorded. */
        if (app.TermsFingerprint is not null
            && app.TermsFingerprint != termsFingerprint.Calculate(contract, app.Opportunity.Period))
            throw new BadRequestException(
                "The contract terms have changed since the artist applied — the artist must re-apply before you can accept");
    }
}
