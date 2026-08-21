using Concertable.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

internal sealed class ApplicationReadDbContext(
    DbContextOptions<ApplicationReadDbContext> options,
    ApplicationConfigurationProvider provider)
    : ReadDbContext(options, provider, Schema.Name), IApplicationReadDbContext
{
    IQueryable<ApplicationEntity> IApplicationReadDbContext.Applications => Query<ApplicationEntity>();
    IQueryable<VerifyPaymentEntity> IApplicationReadDbContext.VerifyPayments => Query<VerifyPaymentEntity>();
}
