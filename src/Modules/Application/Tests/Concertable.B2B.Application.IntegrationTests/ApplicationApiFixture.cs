using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.IntegrationTests;

public sealed class ApplicationApiFixture : ApiFixture
{
    private IApplicationReadDbContext dbContext = null!;

    internal IQueryable<ApplicationEntity> Applications => dbContext.Applications;
    internal IQueryable<VerifyPaymentEntity> PaymentVerifications => dbContext.VerifyPayments;

    protected override void OnReset(IServiceScope scope)
    {
        dbContext = scope.ServiceProvider.GetRequiredService<IApplicationReadDbContext>();
    }
}
