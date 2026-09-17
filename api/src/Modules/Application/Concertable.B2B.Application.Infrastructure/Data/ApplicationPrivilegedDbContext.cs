using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

/// <summary>
/// The Application module's stance for work no human is acting in: the same mapping as
/// <see cref="ApplicationDbContext"/> with no resource filter. Only this module's privileged repositories and
/// seed factories may inject it.
/// </summary>
internal sealed class ApplicationPrivilegedDbContext(
    DbContextOptions<ApplicationPrivilegedDbContext> options,
    ApplicationConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<VerifyPaymentEntity> VerifyPayments => Set<VerifyPaymentEntity>();
    public DbSet<ConcertAvailabilityEntity> ConcertAvailabilities => Set<ConcertAvailabilityEntity>();
    public DbSet<ApplicationAccessGrant> ApplicationAccessGrants => Set<ApplicationAccessGrant>();
}
