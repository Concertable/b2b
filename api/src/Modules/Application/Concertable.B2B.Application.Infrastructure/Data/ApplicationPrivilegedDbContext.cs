using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Application.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="ApplicationDbContext"/>.</summary>
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
