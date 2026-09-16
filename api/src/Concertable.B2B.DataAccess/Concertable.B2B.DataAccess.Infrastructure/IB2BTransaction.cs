using System.Data.Common;

namespace Concertable.B2B.DataAccess.Infrastructure;

/// <summary>
/// The one database transaction a request's writes share. Every module context is built on the same
/// request-scoped <see cref="DbConnection"/>, because EF only lets contexts share a transaction when they
/// also share the connection it was begun on.
/// <para>
/// A protected write that spans modules — accepting an application mints a booking, a contract, their
/// grants and an outbox row across three contexts — has to commit or roll back as one. Independent
/// connections cannot do that, and an ambient scope over several of them escalates to a distributed
/// transaction rather than staying a local one.
/// </para>
/// </summary>
public interface IB2BTransaction
{
    /// <summary>The transaction in progress, or null when the caller is outside one.</summary>
    DbTransaction? Current { get; }

    /// <summary>
    /// Runs the block inside the shared transaction, beginning one if this is the outermost caller and
    /// joining the existing one otherwise. A nested caller never commits: the outermost one does, so a
    /// later failure still rolls the earlier work back.
    /// </summary>
    Task ExecuteAsync(Func<Task> action, CancellationToken cancellationToken = default);

    /// <inheritdoc cref="ExecuteAsync(Func{Task}, CancellationToken)"/>
    Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
}
