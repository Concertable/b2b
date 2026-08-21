using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.Application.Infrastructure.Data;
using Concertable.B2B.IntegrationTests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace Concertable.B2B.Application.IntegrationTests;

public sealed class ApplicationApiFixture : ApiFixture
{
    private IApplicationReadDbContext context = null!;

    internal IQueryable<ApplicationEntity> Applications => context.Applications;
    internal IQueryable<VerifyPaymentEntity> PaymentVerifications => context.VerifyPayments;

    protected override void OnReset(IServiceScope scope)
    {
        context = scope.ServiceProvider.GetRequiredService<IApplicationReadDbContext>();
    }
}
