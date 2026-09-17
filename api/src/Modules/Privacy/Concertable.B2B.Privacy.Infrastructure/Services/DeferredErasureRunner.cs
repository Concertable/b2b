using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging;

namespace Concertable.B2B.Privacy.Infrastructure.Services;

internal sealed class DeferredErasureRunner : IDeferredErasureRunner
{
    private const int SweepBatchSize = 100;

    private readonly ISubjectErasureRepository repository;
    private readonly IScoped<ISubjectErasureService> erasureService;
    private readonly ILogger<DeferredErasureRunner> logger;

    public DeferredErasureRunner(
        ISubjectErasureRepository repository,
        IScoped<ISubjectErasureService> erasureService,
        ILogger<DeferredErasureRunner> logger)
    {
        this.repository = repository;
        this.erasureService = erasureService;
        this.logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var subjectIds = await repository.ListResumableSubjectIdsAsync(SweepBatchSize, ct);
        if (subjectIds.Count == 0)
            return;

        logger.DeferredErasureSweepStarted(subjectIds.Count);

        foreach (var subjectId in subjectIds)
        {
            try
            {
                await erasureService.RunAsync(service => service.RequestErasureAsync(subjectId, ct));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.DeferredErasureFailed(exception, subjectId, Guid.Empty);
            }
        }
    }
}
