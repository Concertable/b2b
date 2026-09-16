using Concertable.B2B.Concert.Contracts.Enums;
using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class InvoiceAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<InvoiceAccessGrant, InvoiceAccessFacet>
{
    protected override string TableName => Schema.Tables.InvoiceAccessGrants;

    protected override string SchemaName => Schema.Name;
}
