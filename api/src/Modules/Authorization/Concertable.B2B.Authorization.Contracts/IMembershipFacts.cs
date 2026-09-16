namespace Concertable.B2B.Authorization.Contracts;

/// <summary>
/// The port Authorization reads memberships through, implemented by the module that owns membership storage.
/// Authorization depends on this contract and never on that module's assemblies; the composition root binds
/// the two, which is what keeps the dependency acyclic.
/// </summary>
public interface IMembershipFacts
{
    Task<MembershipFact?> GetAsync(Guid userId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MembershipFact>> GetAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
