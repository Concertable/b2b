using Concertable.B2B.Authorization.Contracts;

namespace Concertable.B2B.Tenant.Domain.Entities;

public sealed class TenantRoleDefinition
{
    private TenantRoleDefinition() { }

    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = null!;
    public long Version { get; private set; }
    public string? SystemPresetKey { get; private set; }
    public bool IsProtectedOwner { get; private set; }
    public bool IsInvitationAssignable { get; private set; }
    public DateTime? RetiredAt { get; private set; }

    private readonly List<TenantRolePermission> permissions = [];
    public IReadOnlyList<TenantRolePermission> Permissions => permissions.AsReadOnly();

    public static TenantRoleDefinition CreateSystemPreset(Guid tenantId, string presetKey)
    {
        if (!AuthorizationCatalog.Presets.TryGetValue(presetKey, out var preset))
            throw new ArgumentException("Unknown system preset.", nameof(presetKey));
        var role = Create(tenantId, preset.Key, preset.Permissions, true,
            SystemPresetIds.For(tenantId, preset.Key));
        role.SystemPresetKey = preset.Key;
        role.IsProtectedOwner = preset.IsProtectedOwner;
        role.IsInvitationAssignable = preset.IsInvitationAssignable;
        return role;
    }

    public static TenantRoleDefinition CreateCustom(
        Guid tenantId,
        string name,
        bool invitationAssignable,
        IReadOnlyDictionary<TenantPermission, ResourceAudience> grants)
    {
        var role = Create(tenantId, name, grants, false);
        role.IsInvitationAssignable = invitationAssignable;
        return role;
    }

    private static TenantRoleDefinition Create(
        Guid tenantId,
        string name,
        IReadOnlyDictionary<TenantPermission, ResourceAudience> grants,
        bool systemPreset,
        Guid? id = null)
    {
        if (tenantId == Guid.Empty || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new ArgumentException("Invalid role identity or name.");
        var role = new TenantRoleDefinition
        {
            Id = id ?? Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Version = 1,
        };
        role.SetPermissions(grants, systemPreset);
        return role;
    }

    public void Update(
        string name,
        bool invitationAssignable,
        IReadOnlyDictionary<TenantPermission, ResourceAudience> grants)
    {
        EnsureEditable();
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 100)
            throw new ArgumentException("Invalid role name.", nameof(name));
        SetPermissions(grants, false);
        Name = name.Trim();
        IsInvitationAssignable = invitationAssignable;
        Version++;
    }

    public void Retire(DateTime at)
    {
        EnsureEditable();
        RetiredAt = at;
        Version++;
    }

    public bool Has(TenantPermission permission) =>
        permissions.Any(row => row.PermissionKey == permission.Value);

    private void SetPermissions(IReadOnlyDictionary<TenantPermission, ResourceAudience> grants, bool systemPreset)
    {
        foreach (var (permission, audience) in grants)
        {
            if (!TenantPermission.TryParse(permission.Value, out var known)
                || !AuthorizationCatalog.Permissions.TryGetValue(known, out var descriptor)
                || !descriptor.AssignableAudiences.Contains(audience)
                || descriptor.OwnerOnly && !systemPreset)
                throw new ArgumentException("Invalid role permission or audience.");
        }
        permissions.RemoveAll(row => !grants.Keys.Any(key => key.Value == row.PermissionKey));
        foreach (var (permission, audience) in grants)
        {
            var existing = permissions.Find(row => row.PermissionKey == permission.Value);
            if (existing is null)
                permissions.Add(TenantRolePermission.Create(TenantId, Id, permission, audience));
            else
                existing.ChangeAudience(audience);
        }
    }

    private void EnsureEditable()
    {
        if (SystemPresetKey is not null || RetiredAt is not null)
            throw new InvalidOperationException("System or retired roles cannot be edited.");
    }
}
