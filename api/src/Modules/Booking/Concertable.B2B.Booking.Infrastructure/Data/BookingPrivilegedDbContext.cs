using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

/// <summary>The unfiltered, unfenced stance for work no human is acting in. See <see cref="BookingDbContext"/>.</summary>
internal sealed class BookingPrivilegedDbContext(
    DbContextOptions<BookingPrivilegedDbContext> options,
    BookingConfigurationProvider provider)
    : PrivilegedDbContext(options, provider, Schema.Name)
{
    public DbSet<BookingEntity> Bookings => Set<BookingEntity>();
    public DbSet<ContractEntity> Contracts => Set<ContractEntity>();
    public DbSet<BookingAccessGrant> BookingAccessGrants => Set<BookingAccessGrant>();
    public DbSet<ContractAccessGrant> ContractAccessGrants => Set<ContractAccessGrant>();
}
