using Concertable.B2B.Booking.Infrastructure.Data.Configurations;
using Concertable.DataAccess.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Concertable.B2B.Booking.Infrastructure.Data;

internal sealed class BookingConfigurationProvider : IEntityTypeConfigurationProvider
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new BookingEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ContractEntityConfiguration());
        modelBuilder.ApplyConfiguration(new DoorRevenueContractConfiguration());
    }
}
