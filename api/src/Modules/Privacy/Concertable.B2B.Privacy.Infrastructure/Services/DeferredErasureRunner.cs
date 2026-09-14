using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class DeferredErasureRunner : IDeferredErasureRunner
{
    private readonly ISubjectErasureRepository repository;
    private readonly ISubjectErasureService erasureService;
    private readonly ILogger<DeferredErasureRunner> logger;

    public DeferredErasureRunner(
        ISubjectErasureRepository repository,
        ISubjectErasureService erasureService,
        ILogger<DeferredErasureRunner> logger)
    {
        this.repository = repository;
        this.erasureService = erasureService;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var deferred = await repository.ListDeferredAsync(ct);
        if (deferred.Count == 0)
            return;

        logger.DeferredErasureSweepStarted(deferred.Count);

        foreach (var request in deferred)
        {
            // One subject's failure must not strand the rest of the queue: every remaining request still gets
            // its pass, and the failed one stays Deferred for the next sweep rather than being lost.
            try
            {
                await erasureService.ResumeAsync(request, ct);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.DeferredErasureFailed(exception, request.SubjectId, request.Id);
            }
        }
    }
}
