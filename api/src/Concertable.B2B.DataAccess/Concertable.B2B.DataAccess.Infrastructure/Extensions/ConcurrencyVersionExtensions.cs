using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class ConcurrencyVersionExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IConcurrencyVersioned
    {
        public void HasConcurrencyVersion() =>
            builder.Property(entity => entity.Version).IsRowVersion();
    }
}
