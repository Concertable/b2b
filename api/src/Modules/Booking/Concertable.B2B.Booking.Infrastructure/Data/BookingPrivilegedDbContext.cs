using Concertable.B2B.DataAccess.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

/// <summary>
/// The Booking module's stance for work no human is acting in: the same mapping as
/// <see cref="BookingDbContext"/> with no resource filter, so a payment outcome reaches the booking it names.
/// Only this module's privileged repositories and seed factories may inject it.
/// </summary>
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
