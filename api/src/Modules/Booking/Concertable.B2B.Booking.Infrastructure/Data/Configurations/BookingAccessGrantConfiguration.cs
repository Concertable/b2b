using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class BookingAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<BookingAccessGrant, BookingAccessScope>
{
    protected override string TableName => Schema.Tables.BookingAccessGrants;

    protected override string SchemaName => Schema.Name;
}
