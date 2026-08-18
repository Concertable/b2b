using Concertable.B2B.Concert.Application.Interfaces;
using Concertable.B2B.Concert.Application.Steps;
using Concertable.B2B.Concert.Application.Strategies;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.Concert.Infrastructure;
using Concertable.B2B.Concert.Infrastructure.Services.Executors;
using Concertable.B2B.Tenant.Contracts;
using Microsoft.Extensions.Logging;
using Moq;

namespace Concertable.B2B.Concert.UnitTests;

public sealed class CompleteExecutorTests
{
    private readonly Mock<IConcertRepository> concertRepository = new();
    private readonly CompleteExecutor executor;

    public CompleteExecutorTests()
    {
        this.executor = new CompleteExecutor(
            this.concertRepository.Object,
            Mock.Of<IConcertDealStrategyFactory<ICompleteStep>>(),
            Mock.Of<IInvoiceIssuer>(),
            Mock.Of<ITenantModule>(),
            Mock.Of<ISelfBillingAgreementGate>(),
            new ImmediateBehavior(),
            TimeProvider.System,
            Mock.Of<ILogger<CompleteExecutor>>());
    }

    [Fact]
    public async Task CompleteAsync_CallerCancellation_Rethrows()
    {
        using var cancellationSource = new CancellationTokenSource();
        await cancellationSource.CancelAsync();
        var cancellationToken = cancellationSource.Token;
        this.concertRepository
            .Setup(repository => repository.GetByIdForLifecycleAsync(It.IsAny<int>(), cancellationToken))
            .Returns(Task.FromCanceled<ConcertEntity?>(cancellationToken));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => this.executor.CompleteAsync(42, cancellationToken));
    }

    private sealed class ImmediateBehavior : IUnitOfWorkBehavior
    {
        public Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default) => action();

        public Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default) => action();
    }
}
