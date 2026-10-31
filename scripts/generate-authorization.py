#!/usr/bin/env python3
import argparse
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODULES = ("Tenant", "Conversations", "Concert", "Opportunity", "Application", "Booking")
ACCESS_LEVELS = {"Assigned", "All"}
PRESETS = ("Owner", "Manager", "Finance", "Staff", "Door", "Sound")
CS = ROOT / "api/src/Modules/Authorization/Concertable.B2B.Authorization.Contracts"
TS = ROOT / "app/shared/src/features/tenant/permissions.generated.ts"


def quoted(value):
    return json.dumps(value, ensure_ascii=False)


def symbol(name):
    return "".join(part.capitalize() for part in re.split(r"[:_]", name))


def load():
    entries = []
    for module in MODULES:
        path = ROOT / f"api/src/Modules/{module}/authorization.json"
        doc = json.loads(path.read_text(encoding="utf-8"))
        if set(doc) != {"permissions"} or not isinstance(doc["permissions"], list):
            raise ValueError(f"{path}: expected permissions array")
        for item in doc["permissions"]:
            if set(item) != {"name", "label", "ownerOnly", "systemPresets"}:
                raise ValueError(f"{path}: invalid metadata fields for {item.get('name')}")
            entries.append({**item, "category": module})
    names = [item["name"] for item in entries]
    if len(names) != len(set(names)) or len({symbol(name) for name in names}) != len(names):
        raise ValueError("Duplicate permission name or symbol")
    for item in entries:
        name = item["name"]
        if not re.fullmatch(r"[a-z][a-z_]*:[a-z][a-z_]*", name):
            raise ValueError(f"Invalid permission name: {name}")
        if not item["label"].strip():
            raise ValueError(f"Missing label: {name}")
        grants = item["systemPresets"]
        if not isinstance(grants, dict) or "Owner" not in grants or set(grants) - set(PRESETS):
            raise ValueError(f"Invalid preset declaration: {name}")
        if set(grants.values()) - ACCESS_LEVELS:
            raise ValueError(f"Invalid preset access: {name}")
        if item["ownerOnly"] and set(grants) != {"Owner"}:
            raise ValueError(f"Owner-only permission outside Owner preset: {name}")
    return sorted(entries, key=lambda item: item["name"])


def generated(entries):
    canonical = json.dumps(entries, sort_keys=True, separators=(",", ":"), ensure_ascii=False)
    revision = hashlib.sha256(canonical.encode("utf-8")).hexdigest()
    symbols = [symbol(item["name"]) for item in entries]
    lines = ["using System.Collections.Frozen;", "using System.Text.Json.Serialization;", "",
             "namespace Concertable.B2B.Authorization.Contracts;", "",
             "[JsonConverter(typeof(JsonStringEnumConverter<TenantPermission>))]",
             "public enum TenantPermission", "{"]
    for index, (name, item) in enumerate(zip(symbols, entries)):
        lines += [f"    [JsonStringEnumMemberName({quoted(item['name'])})]",
                  f"    {name}{' = 1' if index == 0 else ''},"]
    lines += ["}", "", "public static class TenantPermissionExtensions", "{",
              "    private static readonly FrozenDictionary<TenantPermission, string> Keys =",
              "        new Dictionary<TenantPermission, string>", "        {"]
    lines += [f"            [TenantPermission.{name}] = {quoted(item['name'])},"
              for name, item in zip(symbols, entries)]
    lines += ["        }.ToFrozenDictionary();", "",
              "    private static readonly FrozenDictionary<string, TenantPermission> ByKey =",
              "        Keys.ToFrozenDictionary(pair => pair.Value, pair => pair.Key, StringComparer.Ordinal);", "",
              "    private static readonly FrozenSet<TenantPermission> Declared = Keys.Keys.ToFrozenSet();", "",
              "    extension(TenantPermission permission)", "    {",
              "        public string Key => Keys.TryGetValue(permission, out var key) ? key : string.Empty;", "    }", "",
              "    extension(TenantPermission)", "    {",
              "        public static IReadOnlySet<TenantPermission> All => Declared;", "",
              "        public static bool TryParseKey(string? key, out TenantPermission permission)", "        {",
              "            if (key is not null && ByKey.TryGetValue(key, out permission))",
              "                return true;", "            permission = default;", "            return false;",
              "        }", "    }", "}", ""]
    permission_code = "\n".join(lines)
    catalog = ["#nullable enable", "using System.Collections.Frozen;", "",
               "namespace Concertable.B2B.Authorization.Contracts;", "",
               "public sealed record PermissionDescriptor(",
               "    TenantPermission Permission,", "    string Label,", "    string Category,", "    bool OwnerOnly);",
               "", "public sealed record SystemPresetDescriptor(",
               "    string Key,", "    bool IsProtectedOwner,", "    bool IsInvitationAssignable,",
               "    IReadOnlyDictionary<TenantPermission, ResourceAccess> Permissions);",
               "", "public static class AuthorizationCatalog", "{",
               f'    public const string Revision = "{revision}";', "",
               "    public static IReadOnlyDictionary<TenantPermission, PermissionDescriptor> Permissions { get; } =",
               "        new Dictionary<TenantPermission, PermissionDescriptor>", "        {"]
    for name, item in zip(symbols, entries):
        catalog.append(
            f'            [TenantPermission.{name}] = new(TenantPermission.{name}, {quoted(item["label"])}, '
            f'{quoted(item["category"])}, {str(item["ownerOnly"]).lower()}),')
    catalog += ["        }.ToFrozenDictionary();", "",
                "    public static IReadOnlyDictionary<string, SystemPresetDescriptor> Presets { get; } =",
                "        new Dictionary<string, SystemPresetDescriptor>(StringComparer.Ordinal)", "        {"]
    for preset in PRESETS:
        catalog.append(f'            [{quoted(preset)}] = new({quoted(preset)}, '
                       f'{str(preset == "Owner").lower()}, '
                       f'{str(preset in ("Staff", "Door", "Sound")).lower()}, '
                       "new Dictionary<TenantPermission, ResourceAccess>")
        catalog.append("            {")
        for name, item in zip(symbols, entries):
            access = item["systemPresets"].get(preset)
            if access:
                catalog.append(f'                [TenantPermission.{name}] = ResourceAccess.{access},')
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
