using Concertable.B2B.Concert.Application.Workflow;
using Concertable.B2B.Concert.Application.Workflow.Capabilities;
using Concertable.B2B.Concert.Application.Workflow.Executors;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Domain.Lifecycle;
using Concertable.Kernel;

namespace Concertable.B2B.Concert.Infrastructure.Services.Workflow.Executors;

internal sealed class AcceptExecutor : IAcceptExecutor
{
    private readonly ILifecycleTransitioner transitioner;
    private readonly IConcertWorkflowFactory workflows;
    private readonly IDealResolver dealResolver;
    private readonly IBookingRepository bookingRepository;
    private readonly IContractIssuer contractIssuer;
    private readonly ITermsFingerprintCalculator termsFingerprint;
    private readonly IBookingAdvancer bookingAdvancer;
    private readonly IBackgroundTaskRunner taskRunner;

    public AcceptExecutor(
        ILifecycleTransitioner transitioner,
        IConcertWorkflowFactory workflows,
        IDealResolver dealResolver,
        IBookingRepository bookingRepository,
        IContractIssuer contractIssuer,
        ITermsFingerprintCalculator termsFingerprint,
        IBookingAdvancer bookingAdvancer,
        IBackgroundTaskRunner taskRunner)
    {
        this.transitioner = transitioner;
        this.workflows = workflows;
        this.dealResolver = dealResolver;
        this.bookingRepository = bookingRepository;
        this.contractIssuer = contractIssuer;
        this.termsFingerprint = termsFingerprint;
        this.bookingAdvancer = bookingAdvancer;
        this.taskRunner = taskRunner;
    }

    public async Task<UnitResult<AcceptApplicationError>> AcceptAsync(
        int applicationId,
        string? paymentMethodId,
        ESignatureRequest eSignature,
        CancellationToken ct = default)
    {
        var transition = await transitioner.TransitionAsync<AcceptApplicationError>(
            applicationId,
            Trigger.Accept,
            error => (AcceptApplicationError)new AcceptApplicationError.TransitionFailure(error),
            async app =>
        {
            var deal = await dealResolver.ResolveByApplicationIdAsync(app.Id);
            var terms = VerifyTermsUnchanged(app, deal);
            if (terms.TryGetError(out var termsError))
                return UnitResult.Failure(termsError);

            var workflow = workflows.Create(app.DealType);
            var acceptance = workflow switch
            {
                IAcceptsPaid w when paymentMethodId is not null => await w.Accept.ExecuteAsync(app, paymentMethodId, ct),
                IAcceptsPaid => UnitResult.Failure<AcceptApplicationError>(new AcceptApplicationError.PaymentMethodRequired()),
                IAcceptsSimple w => await w.Accept.ExecuteAsync(app, ct),
                _ => UnitResult.Failure<AcceptApplicationError>(new AcceptApplicationError.UnsupportedDeal(workflow.Type))
            };
            if (acceptance.TryGetError(out var acceptanceError))
                return UnitResult.Failure(acceptanceError);

            var booking = await bookingRepository.GetByApplicationIdAsync(app.Id)
                ?? throw new InvalidOperationException($"Application {app.Id} has no booking after acceptance.");
            app.Accept(booking);
            await contractIssuer.IssueAsync(app, booking, eSignature);

            await taskRunner.RunAsync<IApplicationRepository>(
                (repo, runCt) => repo.RejectAllExceptAsync(app.OpportunityId, app.Id));
            return UnitResult.Success<AcceptApplicationError>();
        }, ct);

        var result = transition.Bind(_ => UnitResult.Success<AcceptApplicationError>());
        if (result.IsFailure)
            return result;

        await bookingAdvancer.AdvanceIfReadyAsync(applicationId);
        return result;
    }

    private UnitResult<AcceptApplicationError> VerifyTermsUnchanged(ApplicationEntity app, IDeal deal) =>
        app.TermsFingerprint == termsFingerprint.Calculate(deal, app.Opportunity.Period)
            ? UnitResult.Success<AcceptApplicationError>()
            : UnitResult.Failure<AcceptApplicationError>(new AcceptApplicationError.TermsChanged());
}
