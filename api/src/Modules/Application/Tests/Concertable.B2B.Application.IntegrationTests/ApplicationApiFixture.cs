using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Concertable.Testing.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.IntegrationTests;

public sealed class ApplicationApiFixture : ApiFixture
{
    private IApplicationReadDbContext readDbContext = null!;

    internal ConcurrencyConflictInterceptor Conflicts { get; } = new();

    internal IQueryable<ApplicationEntity> Applications => readDbContext.Applications;
    internal IQueryable<ConcertAvailabilityEntity> ConcertAvailabilities => readDbContext.ConcertAvailabilities;
    internal IQueryable<VerifyPaymentEntity> PaymentVerifications => readDbContext.VerifyPayments;

    /// <summary>
    /// Commits <paramref name="competingChange"/> between the next application transition's read and its
    /// update, so that transition loses the race and has to rerun against the winner's state.
    /// </summary>
    internal void ArmApplicationConflict(Func<Task> competingChange) =>
        Conflicts.ArmOnce<ApplicationEntity>(competingChange);

    protected override void OnConfigureServices(IServiceCollection services)
    {
        services.AddResettables(Conflicts);
        services.ConfigureDbContext<ApplicationDbContext>(
            (_, options) => options.AddInterceptors(Conflicts));
    }

    protected override void OnReset(IServiceScope scope) =>
        readDbContext = scope.ServiceProvider.GetRequiredService<IApplicationReadDbContext>();
}
