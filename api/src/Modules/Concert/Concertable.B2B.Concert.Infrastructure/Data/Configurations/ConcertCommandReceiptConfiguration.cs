using Concertable.B2B.Concert.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Concert.Infrastructure.Data.Configurations;

internal sealed class ConcertCommandReceiptConfiguration
    : ResourceCommandReceiptConfiguration<ConcertCommandReceipt>
{
    protected override string TableName => Schema.Tables.ConcertCommandReceipts;

    protected override string SchemaName => Schema.Name;
}
