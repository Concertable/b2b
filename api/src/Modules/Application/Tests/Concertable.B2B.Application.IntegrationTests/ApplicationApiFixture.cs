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

    internal IQueryable<ApplicationEntity> Applications => readDbContext.Applications;
    internal IQueryable<ConcertAvailabilityEntity> ConcertAvailabilities => readDbContext.ConcertAvailabilities;
    internal IQueryable<VerifyPaymentEntity> PaymentVerifications => readDbContext.VerifyPayments;

    protected override void OnReset(IServiceScope scope) =>
        readDbContext = scope.ServiceProvider.GetRequiredService<IApplicationReadDbContext>();
}
