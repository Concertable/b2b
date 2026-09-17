using Reunion;
using Concertable.B2B.Privacy.Application.DTOs;
using Concertable.B2B.Privacy.Application.Interfaces;
using Concertable.B2B.Privacy.Domain.Lifecycle;
using Concertable.B2B.Privacy.Infrastructure.Services;
using Concertable.DataAccess.Application;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Concertable.B2B.Privacy.UnitTests;

public sealed class DeferredErasureRunnerTests
{
    private readonly Mock<ISubjectErasureRepository> repository = new();
    private readonly Mock<IScoped<ISubjectErasureService>> scoped = new();
    private readonly DeferredErasureRunner runner;

    public DeferredErasureRunnerTests()
    {
        this.runner = new DeferredErasureRunner(
            repository.Object,
            scoped.Object,
            NullLogger<DeferredErasureRunner>.Instance);
    }

    [Fact]
    public async Task RunAsync_NoResumableSubjects_DoesNotOpenAScope()
    {
        // Arrange
        repository
            .Setup(r => r.ListResumableSubjectIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await runner.RunAsync();

        // Assert
        scoped.Verify(
            s => s.RunAsync(It.IsAny<Func<ISubjectErasureService, Task<Result<SubjectErasureRequestDto, ErasureTransitionError>>>>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_OneSubjectThrows_StillProcessesTheRest()
    {
        // Arrange
        var failing = Guid.NewGuid();
        var healthy = Guid.NewGuid();
        repository
            .Setup(r => r.ListResumableSubjectIdsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([failing, healthy]);

        var processed = new List<Guid>();
        scoped
            .Setup(s => s.RunAsync(It.IsAny<Func<ISubjectErasureService, Task<Result<SubjectErasureRequestDto, ErasureTransitionError>>>>()))
            .Returns((Func<ISubjectErasureService, Task<Result<SubjectErasureRequestDto, ErasureTransitionError>>> work) =>
            {
                var service = new Mock<ISubjectErasureService>();
                service
                    .Setup(s => s.RequestErasureAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                    .Returns((Guid subjectId, CancellationToken _) =>
                    {
                        processed.Add(subjectId);
                        return subjectId == failing
                            ? throw new InvalidOperationException("boom")
                            : Task.FromResult<Result<SubjectErasureRequestDto, ErasureTransitionError>>(
                                new SubjectErasureRequestDto
                                {
                                    Id = Guid.NewGuid(),
                                    SubjectId = subjectId,
                                    State = ErasureState.Completed,
                                    RequestedAtUtc = DateTime.UtcNow,
                                });
                    });
                return work(service.Object);
            });

        // Act
        await runner.RunAsync();

        // Assert
        Assert.Equal([failing, healthy], processed);
    }
}
