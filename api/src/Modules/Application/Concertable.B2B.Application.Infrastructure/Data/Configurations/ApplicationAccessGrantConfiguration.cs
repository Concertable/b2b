using Concertable.B2B.Application.Contracts.Enums;
using Concertable.B2B.Application.Domain.Entities;
using Concertable.B2B.DataAccess.Infrastructure;

namespace Concertable.B2B.Application.Infrastructure.Data.Configurations;

internal sealed class ApplicationAccessGrantConfiguration
    : ResourceAccessGrantConfiguration<ApplicationAccessGrant, ApplicationAccessFacet>
{
    protected override string TableName => Schema.Tables.ApplicationAccessGrants;

    protected override string SchemaName => Schema.Name;
}
