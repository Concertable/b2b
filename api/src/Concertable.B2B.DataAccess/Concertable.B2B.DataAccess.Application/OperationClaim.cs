namespace Concertable.B2B.DataAccess.Application;

/// <summary>
/// Anchors a long-running operation to the row it acts on, so a retried request resumes the same
/// operation instead of starting a second one. Composed into an entity — one instance per claimable
/// operation — and mapped as an owned entity type sharing the entity's table, so a claim travels
/// inside the owner's concurrency token.
/// </summary>
/// <remarks>
/// <see cref="Claim()"/> mints on first use and resumes thereafter; <see cref="Claim(Guid)"/> takes an
/// id the caller already holds. Both reject a rival operation. <see cref="IsHeldBy"/> verifies without
/// claiming. Claiming is internal so only an owning aggregate can take a claim, behind whatever
/// transition gates it; everything else reads the mapped <see cref="OperationId"/>.
/// </remarks>
public sealed class OperationClaim
{
    public Guid? OperationId { get; private set; }

    public bool IsHeldBy(Guid operationId) =>
        operationId != Guid.Empty && OperationId == operationId;

    internal Guid Claim() => OperationId ?? Claim(Guid.CreateVersion7());

    internal Guid Claim(Guid operationId)
    {
        if (operationId == Guid.Empty)
            throw new ArgumentException("An operation id is required.", nameof(operationId));
        if (OperationId is { } held && held != operationId)
            throw new InvalidOperationException(
                $"The row is already claimed by operation {held}, not {operationId}.");

        OperationId = operationId;
        return operationId;
    }
}
