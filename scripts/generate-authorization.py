#!/usr/bin/env python3
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODULES = ("Tenant", "Conversations", "Concert", "Opportunity", "Application", "Booking")
ACCESS_LEVELS = {"Assigned", "All"}
ACTIVITIES = {"VenueOperator", "Artist", "Promoter"}
CS = ROOT / "api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts"
TENANT_CS = ROOT / "api/src/Modules/Tenant/Concertable.B2B.Tenant.Contracts"
TS = ROOT / "app/shared/src/features/tenant/permissions.generated.ts"


def quoted(value):
    return json.dumps(value, ensure_ascii=False)


def symbol(name):
    return "".join(part.capitalize() for part in re.split(r"[:_]", name))


def load():
    entries = []
    system_roles = None
    for module in MODULES:
        path = ROOT / f"api/src/Modules/{module}/authorization.json"
        doc = json.loads(path.read_text(encoding="utf-8"))
        allowed = {"permissions", "systemRoles"} if module == "Tenant" else {"permissions"}
        if set(doc) - allowed or "permissions" not in doc or not isinstance(doc["permissions"], list):
            raise ValueError(f"{path}: expected a permissions array" + (
                " and an optional systemRoles object" if module == "Tenant" else ""))
        if module == "Tenant":
            if "systemRoles" not in doc or not isinstance(doc["systemRoles"], dict) or not doc["systemRoles"]:
                raise ValueError(f"{path}: must declare systemRoles")
            system_roles = doc["systemRoles"]
        for item in doc["permissions"]:
            if set(item) != {"name", "label", "ownerOnly", "systemPresets"}:
                raise ValueError(f"{path}: invalid metadata fields for {item.get('name')}")
            entries.append({**item, "category": module})
    if system_roles is None:
        raise ValueError("Tenant authorization.json did not declare systemRoles")

    names = [item["name"] for item in entries]
    if len(names) != len(set(names)) or len({symbol(name) for name in names}) != len(names):
        raise ValueError("Duplicate permission name or symbol")

    protected_roles = [role for role, declaration in system_roles.items() if declaration.get("protected")]
    if "Owner" not in system_roles or protected_roles != ["Owner"]:
        raise ValueError("Owner must be declared and be the only protected system role")
    for role, declaration in system_roles.items():
        if set(declaration) != {"protected", "invitationAssignable", "requiredActivity"}:
            raise ValueError(f"Invalid system role declaration: {role}")
        if declaration["requiredActivity"] is not None and declaration["requiredActivity"] not in ACTIVITIES:
            raise ValueError(f"Invalid requiredActivity for system role {role}: {declaration['requiredActivity']}")

    for item in entries:
        name = item["name"]
        if not re.fullmatch(r"[a-z][a-z_]*:[a-z][a-z_]*", name):
            raise ValueError(f"Invalid permission name: {name}")
        if not item["label"].strip():
            raise ValueError(f"Missing label: {name}")
        grants = item["systemPresets"]
        if not isinstance(grants, dict) or "Owner" not in grants or set(grants) - set(system_roles):
            raise ValueError(f"Invalid system role declaration for permission: {name}")
        if set(grants.values()) - ACCESS_LEVELS:
            raise ValueError(f"Invalid preset access: {name}")
        if item["ownerOnly"] and set(grants) != {"Owner"}:
            raise ValueError(f"Owner-only permission outside Owner preset: {name}")
    return sorted(entries, key=lambda item: item["name"]), system_roles


def generated(entries, system_roles):
    canonical = json.dumps(
        {"permissions": entries, "systemRoles": system_roles},
        sort_keys=True, separators=(",", ":"), ensure_ascii=False)
    fingerprint = hashlib.sha256(canonical.encode("utf-8")).hexdigest()
    symbols = [symbol(item["name"]) for item in entries]

    lines = ["using System.Collections.Frozen;", "using System.Text.Json.Serialization;", "",
             "namespace Concertable.B2B.Authorization.Contracts;", "",
             "[JsonConverter(typeof(JsonStringEnumConverter<TenantPermission>))]",
             "public enum TenantPermission", "{"]
    for index, (name, item) in enumerate(zip(symbols, entries)):
        lines += [f"    [JsonStringEnumMemberName({quoted(item['name'])})]",
                  f"    {name}{' = 1' if index == 0 else ''},"]
    lines += ["}", "", "public static class TenantPermissionExtensions", "{",
              "    private static readonly FrozenDictionary<TenantPermission, string> Names =",
              "        new Dictionary<TenantPermission, string>", "        {"]
    lines += [f"            [TenantPermission.{name}] = {quoted(item['name'])},"
              for name, item in zip(symbols, entries)]
    lines += ["        }.ToFrozenDictionary();", "",
              "    private static readonly FrozenDictionary<string, TenantPermission> ByName =",
              "        Names.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);", "",
              "    private static readonly FrozenSet<TenantPermission> Declared = Names.Keys.ToFrozenSet();", "",
              "    private static readonly FrozenDictionary<TenantPermission, string> Labels =",
              "        new Dictionary<TenantPermission, string>", "        {"]
    lines += [f"            [TenantPermission.{name}] = {quoted(item['label'])},"
              for name, item in zip(symbols, entries)]
    lines += ["        }.ToFrozenDictionary();", "",
              "    private static readonly FrozenDictionary<TenantPermission, string> Categories =",
              "        new Dictionary<TenantPermission, string>", "        {"]
    lines += [f"            [TenantPermission.{name}] = {quoted(item['category'])},"
              for name, item in zip(symbols, entries)]
    lines += ["        }.ToFrozenDictionary();", "",
              "    private static readonly FrozenSet<TenantPermission> OwnerOnlyPermissions =",
              "        new HashSet<TenantPermission>", "        {"]
    lines += [f"            TenantPermission.{name}," for name, item in zip(symbols, entries) if item["ownerOnly"]]
    lines += ["        }.ToFrozenSet();", "",
              "    extension(TenantPermission permission)", "    {",
              "        public string Name => Names.TryGetValue(permission, out var name) ? name : string.Empty;",
              "        public string Label => Labels.TryGetValue(permission, out var label) ? label : string.Empty;",
              "        public string Category => Categories.TryGetValue(permission, out var category) ? category : string.Empty;",
              "        public bool IsOwnerOnly => OwnerOnlyPermissions.Contains(permission);",
              "    }", "",
              "    extension(TenantPermission)", "    {",
              "        public static IReadOnlySet<TenantPermission> All => Declared;", "",
              "        public static bool TryParseName(string? name, out TenantPermission permission)", "        {",
              "            if (name is not null && ByName.TryGetValue(name, out permission))",
              "                return true;", "            permission = default;", "            return false;",
              "        }", "    }", "}", ""]
    permission_code = "\n".join(lines)

    role_names = list(system_roles)
    role_lines = ["using System.Collections.Frozen;", "using System.Security.Cryptography;", "using System.Text;",
                  "using Concertable.Kernel.ValueObjects;", "",
                  "namespace Concertable.B2B.Tenant.Contracts;", "",
                  "public enum SystemRole", "{"]
    for index, role in enumerate(role_names):
        role_lines.append(f"    {role}{' = 1' if index == 0 else ''},")
    role_lines += ["}", "", "public static class SystemRoleExtensions", "{",
                   "    private static readonly FrozenDictionary<SystemRole, IReadOnlyDictionary<TenantPermission, ResourceAccess>> GrantsByRole =",
                   "        new Dictionary<SystemRole, IReadOnlyDictionary<TenantPermission, ResourceAccess>>", "        {"]
    for role in role_names:
        role_lines.append(f"            [SystemRole.{role}] = new Dictionary<TenantPermission, ResourceAccess>")
        role_lines.append("            {")
        for name, item in zip(symbols, entries):
            access = item["systemPresets"].get(role)
            if access:
                role_lines.append(f"                [TenantPermission.{name}] = ResourceAccess.{access},")
        role_lines.append("            }.ToFrozenDictionary(),")
    role_lines += ["        }.ToFrozenDictionary();", "",
                   "    private static readonly FrozenSet<SystemRole> ProtectedRoles =",
                   "        new HashSet<SystemRole>", "        {"]
    role_lines += [f"            SystemRole.{role}," for role, declaration in system_roles.items()
                   if declaration["protected"]]
    role_lines += ["        }.ToFrozenSet();", "",
                   "    private static readonly FrozenSet<SystemRole> InvitationAssignableRoles =",
                   "        new HashSet<SystemRole>", "        {"]
    role_lines += [f"            SystemRole.{role}," for role, declaration in system_roles.items()
                   if declaration["invitationAssignable"]]
    role_lines += ["        }.ToFrozenSet();", "",
                   "    private static readonly FrozenDictionary<SystemRole, TenantBusinessActivityKind?> RequiredActivityByRole =",
                   "        new Dictionary<SystemRole, TenantBusinessActivityKind?>", "        {"]
    for role, declaration in system_roles.items():
        required = declaration["requiredActivity"]
        value = "null" if required is None else f"TenantBusinessActivityKind.{required}"
        role_lines.append(f"            [SystemRole.{role}] = {value},")
    role_lines += ["        }.ToFrozenDictionary();", "",
                   "    extension(SystemRole role)", "    {",
                   "        public IReadOnlyDictionary<TenantPermission, ResourceAccess> Grants => GrantsByRole[role];",
                   "        public bool IsProtected => ProtectedRoles.Contains(role);",
                   "        public bool IsInvitationAssignable => InvitationAssignableRoles.Contains(role);",
                   "        public TenantBusinessActivityKind? RequiredActivity => RequiredActivityByRole[role];", "",
                   "        public Guid IdFor(Guid tenantId)", "        {",
                   "            if (tenantId == Guid.Empty)",
                   "                throw new ArgumentException(\"Unknown tenant or system role.\");",
                   "            var input = tenantId.ToByteArray().Concat(Encoding.UTF8.GetBytes(role.ToString())).ToArray();",
                   "            var hash = SHA256.HashData(input);",
                   "            return new Guid(hash.AsSpan(0, 16));",
                   "        }", "    }", "}", "",
                   "public static class SystemRoles", "{",
                   f"    public static Sha256Digest Fingerprint {{ get; }} = Sha256Digest.From({quoted(fingerprint)});", "",
                   "    public static IReadOnlyList<SystemRole> All { get; } =", "    ["]
    role_lines += [f"        SystemRole.{role}," for role in role_names]
    role_lines += ["    ];", "",
                   "    public static IReadOnlyList<SystemRole> AvailableTo(IEnumerable<TenantBusinessActivityKind> activities)",
                   "    {",
                   "        var active = activities as ICollection<TenantBusinessActivityKind> ?? activities.ToArray();",
                   "        return [.. All.Where(role => role.RequiredActivity is not { } required || active.Contains(required))];",
                   "    }", "}", ""]
    system_role_code = "\n".join(role_lines)

    union = "export type TenantPermission =\n" + "  " + " |\n  ".join(
        quoted(item["name"]) for item in entries) + ";\n"
    return {
        CS / "TenantPermission.cs": permission_code,
        TENANT_CS / "SystemRole.generated.cs": system_role_code,
        TS: union,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    entries, system_roles = load()
    outputs = generated(entries, system_roles)
    stale = [str(path.relative_to(ROOT)) for path, text in outputs.items()
             if not path.exists() or path.read_text(encoding="utf-8") != text]
    if args.check:
        if stale:
            raise SystemExit("Stale generated authorization files: " + ", ".join(stale))
        return
    for path, text in outputs.items():
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")
    print("Generated authorization catalog and permission types")


if __name__ == "__main__":
    main()
