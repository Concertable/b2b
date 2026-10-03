#!/usr/bin/env python3
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODULES = ("Tenant", "Conversations", "Concert", "Opportunity", "Application", "Booking")
AUDIENCES = {"AssignedResources", "TenantResources"}
PRESETS = ("Owner", "Manager", "Finance", "Staff", "Door", "Sound")
BINDINGS = {
    "tenant": {"membership"},
    "conversation": {"conversation_grant", "principal_administration"},
    "concert": {"concert_grant", "venue_principal", "either_principal", "principal_administration"},
    "opportunity": {"opportunity_grant", "venue_principal"},
    "application": {"application_grant", "artist_principal", "venue_principal", "principal_administration"},
    "booking": {"booking_grant", "either_principal", "principal_administration"},
    "invoice": {"invoice_grant"},
    "contract": {"contract_grant"},
}
SCOPES = {"Read", "SendMessages", "Summary", "Operations", "Finance", "Proposal"}
CS = ROOT / "api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts"
TS = ROOT / "app/shared/src/features/tenant/permissions.generated.ts"


def quoted(value):
    return json.dumps(value, ensure_ascii=False)


def load():
    entries = []
    for module in MODULES:
        path = ROOT / f"api/src/Modules/{module}/authorization.json"
        doc = json.loads(path.read_text(encoding="utf-8"))
        if set(doc) != {"permissions"} or not isinstance(doc["permissions"], list):
            raise ValueError(f"{path}: expected permissions array")
        for item in doc["permissions"]:
            expected = {"name", "symbol", "label", "category", "resourceBindings",
                        "assignableAudiences", "ownerOnly", "systemPresets"}
            if set(item) != expected:
                raise ValueError(f"{path}: invalid metadata fields for {item.get('name')}")
            if item["category"] != module:
                raise ValueError(f"{path}: category does not match owner")
            entries.append(item)
    keys = [item["name"] for item in entries]
    symbols = [item["symbol"] for item in entries]
    if len(keys) != len(set(keys)) or len(symbols) != len(set(symbols)):
        raise ValueError("Duplicate permission name or symbol")
    for item in entries:
        key = item["name"]
        if not re.fullmatch(r"[a-z][a-z0-9_]*(?:\.[a-z][a-z0-9_]*)+", key):
            raise ValueError(f"Invalid permission key: {key}")
        if not re.fullmatch(r"[A-Z][A-Za-z0-9]*", item["symbol"]):
            raise ValueError(f"Invalid symbol: {key}")
        if not item["label"].strip():
            raise ValueError(f"Missing label: {key}")
        bindings = item["resourceBindings"]
        if not bindings:
            raise ValueError(f"Missing resource binding: {key}")
        seen_bindings = set()
        for binding in bindings:
            if set(binding) != {"resource", "facet", "policy", "requiresScopes"}:
                raise ValueError(f"Invalid resource binding fields: {key}")
            resource, facet, policy = binding["resource"], binding["facet"], binding["policy"]
            if resource not in BINDINGS or policy not in BINDINGS[resource]:
                raise ValueError(f"Unknown resource policy binding: {key}")
            if facet is not None and facet not in SCOPES:
                raise ValueError(f"Unknown facet: {key}")
            scopes = binding["requiresScopes"]
            if set(scopes) - SCOPES or len(set(scopes)) != len(scopes):
                raise ValueError(f"Unknown or duplicate scope: {key}")
            binding_key = (resource, facet)
            if binding_key in seen_bindings:
                raise ValueError(f"Duplicate resource/facet binding: {key}")
            seen_bindings.add(binding_key)
        audiences = item["assignableAudiences"]
        if not audiences or set(audiences) - AUDIENCES or len(set(audiences)) != len(audiences):
            raise ValueError(f"Invalid audiences: {key}")
        grants = item["systemPresets"]
        if not isinstance(grants, dict) or "Owner" not in grants or set(grants) - set(PRESETS):
            raise ValueError(f"Invalid preset declaration: {key}")
        if any(audience not in audiences for audience in grants.values()):
            raise ValueError(f"Invalid preset audience: {key}")
        if item["ownerOnly"] and set(grants) != {"Owner"}:
            raise ValueError(f"Owner-only permission outside Owner preset: {key}")
    return sorted(entries, key=lambda item: item["name"])


def generated(entries):
    canonical = json.dumps(entries, sort_keys=True, separators=(",", ":"), ensure_ascii=False)
    revision = hashlib.sha256(canonical.encode("utf-8")).hexdigest()
    symbols = [item["symbol"] for item in entries]
    lines = ["using System.Collections.Frozen;", "", "namespace Concertable.B2B.Authorization.Contracts;",
             "", "public readonly record struct TenantPermission", "{"]
    lines += [f'    public const string {s}Name = {quoted(item["name"])};'
              for s, item in zip(symbols, entries)]
    lines += [""] + [f"    public static TenantPermission {s} {{ get; }} = new({s}Name);"
                      for s in symbols]
    lines += ["", "    public static IReadOnlySet<TenantPermission> All { get; } = new[]",
              "    {", "        " + ", ".join(symbols) + ",", "    }.ToFrozenSet();",
              "", "    private static readonly FrozenDictionary<string, TenantPermission> ByValue =",
              "        All.ToFrozenDictionary(permission => permission.Value, StringComparer.Ordinal);",
              "", "    private TenantPermission(string value) => Value = value;",
              "    public string Value { get; }",
              "", "    public static bool TryParse(string? value, out TenantPermission permission)",
              "    {", "        if (value is not null && ByValue.TryGetValue(value, out permission))",
              "            return true;", "        permission = default;", "        return false;", "    }",
              "", "    public override string ToString() => Value ?? string.Empty;", "}", ""]
    permission_code = "\n".join(lines)
    catalog = ["#nullable enable", "using System.Collections.Frozen;", "", "namespace Concertable.B2B.Authorization.Contracts;",
               "", "public sealed record PermissionResourceBinding(",
               "    string Resource,", "    string? Facet,", "    string Policy,",
               "    IReadOnlyList<string> RequiresScopes);",
               "", "public sealed record PermissionDescriptor(",
               "    TenantPermission Permission,", "    string Label,", "    string Category,",
               "    IReadOnlyList<PermissionResourceBinding> ResourceBindings,",
               "    IReadOnlyList<ResourceAudience> AssignableAudiences,", "    bool OwnerOnly);",
               "", "public sealed record SystemPresetDescriptor(",
               "    string Key,", "    bool IsProtectedOwner,", "    bool IsInvitationAssignable,",
               "    IReadOnlyDictionary<TenantPermission, ResourceAudience> Permissions);",
               "", "public static class AuthorizationCatalog", "{",
               f'    public const string Revision = "{revision}";', "",
               "    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =",
               "        new Dictionary<TenantPermission, PermissionDescriptor>", "        {"]
    for item in entries:
        sym = item["symbol"]
        aud = ", ".join("ResourceAudience." + a for a in item["assignableAudiences"])
        bindings = []
        for binding in item["resourceBindings"]:
            scopes = ", ".join(quoted(value) for value in binding["requiresScopes"])
            facet = "null" if binding["facet"] is None else quoted(binding["facet"])
            bindings.append(
                f'new({quoted(binding["resource"])}, {facet}, {quoted(binding["policy"])}, [{scopes}])')
        binding_values = ", ".join(bindings)
        catalog.append(
            f'            [TenantPermission.{sym}] = new(TenantPermission.{sym}, {quoted(item["label"])}, '
            f'{quoted(item["category"])}, [{binding_values}], [{aud}], '
            f'{str(item["ownerOnly"]).lower()}),')
    catalog += ["        }.ToFrozenDictionary();", "",
                "    public static IReadOnlyDictionary<string, SystemPresetDescriptor> Presets { get; } =",
                "        new Dictionary<string, SystemPresetDescriptor>(StringComparer.Ordinal)", "        {"]
    for preset in PRESETS:
        catalog.append(f'            [{quoted(preset)}] = new({quoted(preset)}, '
                       f'{str(preset == "Owner").lower()}, '
                       f'{str(preset in ("Staff", "Door", "Sound")).lower()}, '
                       "new Dictionary<TenantPermission, ResourceAudience>")
        catalog.append("            {")
        for item in entries:
            audience = item["systemPresets"].get(preset)
            if audience:
                catalog.append(f'                [TenantPermission.{item["symbol"]}] = ResourceAudience.{audience},')
        catalog += ["            }.ToFrozenDictionary()),"]
    catalog += ["        }.ToFrozenDictionary(StringComparer.Ordinal);", "}", ""]
    union = "export type TenantPermission =\n" + "  " + " |\n  ".join(
        quoted(item["name"]) for item in entries) + ";\n"
    return {
        CS / "TenantPermission.cs": permission_code,
        CS / "AuthorizationCatalog.generated.cs": "\n".join(catalog),
        TS: union,
    }


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    outputs = generated(load())
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
