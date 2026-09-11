namespace Concertable.B2B.Admin.Domain.Entities;

/// <summary>
/// Grants platform-admin authority to an Auth <c>sub</c> — the source of truth for "who may act as a
/// Concertable admin". Keyed directly by <see cref="Sub"/>.
/// </summary>
public sealed class AdminProfileEntity
{
    private AdminProfileEntity() { }

    public AdminProfileEntity(Guid sub)
    {
        Sub = sub;
    }

    /// <summary>The admin's Auth <c>sub</c>. A plain primitive FK — Auth owns the identity, B2B owns membership.</summary>
    public Guid Sub { get; private set; }
}
