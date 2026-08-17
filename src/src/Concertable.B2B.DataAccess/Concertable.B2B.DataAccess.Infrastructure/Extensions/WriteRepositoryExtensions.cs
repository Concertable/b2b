using Concertable.DataAccess.Application;
using Concertable.DataAccess.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;
using Reunion;

namespace Concertable.B2B.DataAccess.Infrastructure.Extensions;

public static class WriteRepositoryExtensions
{
    public static async Task<Result<TEntity, DuplicateKeyError>> TryInsertAsync<TEntity>(
        this IWriteRepository<TEntity> repository,
        TEntity entity,
        CancellationToken ct = default)
        where TEntity : class
    {
        try
        {
            var inserted = await repository.InsertAsync(entity, ct);
            return Result.Success<TEntity, DuplicateKeyError>(inserted);
        }
        catch (DbUpdateException ex) when (ex.IsDuplicateKey())
        {
            ex.DiscardFailedChanges();
            return Result.Failure<TEntity, DuplicateKeyError>(new DuplicateKeyError());
        }
    }
}

public sealed record DuplicateKeyError;
