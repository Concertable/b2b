using System.Linq.Expressions;
using Concertable.B2B.DataAccess.Application;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class OperationClaimExtensions
{
    extension<TEntity>(EntityTypeBuilder<TEntity> builder)
        where TEntity : class
    {
        /// <summary>
        /// Maps a claim an operation takes at some point in the row's life, so the column stays nullable
        /// and its uniqueness is enforced only over claimed rows.
        /// </summary>
        public void OwnsClaim(
            Expression<Func<TEntity, OperationClaim?>> claim,
            string columnName) =>
            builder.OwnsOne(claim, owned =>
            {
                owned.Property(value => value.OperationId).HasColumnName(columnName);
                owned.HasIndex(value => value.OperationId)
                    .IsUnique()
                    .HasFilter($"[{columnName}] IS NOT NULL");
            });

        /// <summary>
        /// Maps a claim the row is created holding, so the column is non-nullable and every row
        /// participates in the unique index.
        /// </summary>
        public void OwnsRequiredClaim(
            Expression<Func<TEntity, OperationClaim?>> claim,
            string columnName) =>
            builder.OwnsOne(claim, owned =>
            {
                owned.Property(value => value.OperationId).HasColumnName(columnName).IsRequired();
                owned.HasIndex(value => value.OperationId).IsUnique();
            });
    }

    extension<TEntity>(IQueryable<TEntity> source)
        where TEntity : class, IHasCancellationClaim
    {
        /// <summary>
        /// The row whose cancellation is held by <paramref name="operationId"/>, for correlating a
        /// financial outcome back to the cancellation that started it.
        /// </summary>
        public IQueryable<TEntity> WithCancellationClaim(Guid operationId) =>
            source.Where(entity => entity.Cancellation.OperationId == operationId);
    }
}
