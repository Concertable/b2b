using Concertable.B2B.Booking.Contracts.Enums;
using Concertable.B2B.Booking.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Booking.Infrastructure.Data.Configurations;

internal sealed class ContractAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<ContractAccessGrant, ContractAccessFacet>
{
    protected override string TableName => Schema.Tables.ContractAccessGrants;

    protected override string SchemaName => Schema.Name;
}
