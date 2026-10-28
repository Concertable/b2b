# Concertable.B2B

Data service — venue↔artist booking + settlement. B2B's DbContext stances, filtered-entity list and `DealType` strategy/workflow rosters: @./CODE_PATTERNS.md. Internal design → [`ARCHITECTURE.md`](./ARCHITECTURE.md); deal/contract/workflow → [`api/src/Modules/Deal/ARCHITECTURE.md`](./api/src/Modules/Deal/ARCHITECTURE.md) + [`api/src/Modules/Concert/AGENTS.md`](./api/src/Modules/Concert/AGENTS.md); legal/VAT → [`api/src/Modules/Deal/LEGAL_REQUIREMENTS.md`](./api/src/Modules/Deal/LEGAL_REQUIREMENTS.md).

## Product direction — configurable work within fixed lifecycle routes

Before designing a deal or lifecycle change, read the central [configurable-workflow product direction](https://github.com/Concertable/docs/blob/main/product/CONFIGURABLE_DEAL_WORKFLOWS.md), especially its [entry routes and stage ownership](https://github.com/Concertable/docs/blob/main/product/CONFIGURABLE_DEAL_WORKFLOWS.md#applications-and-direct-offers): **Opportunity → Application → Booking → Concert**, or **Direct Invitation → Booking → Concert**. These are the fixed product routes; configuration composes supported work inside their owning stages, not an arbitrary replacement lifecycle.

The local [Deal target design](./api/src/Modules/Deal/ARCHITECTURE.md#5-configurable-deals--target-design) owns the engineering mapping: finite code-owned semantics, versioned configuration and immutable accepted terms. The four current deals become presets, not the permanent limit of the product. Customer-authored configurations and the direct-invitation route are targets, not shipped capabilities; check current source before asserting support.

The [nightclub/DJ benchmark](https://github.com/Concertable/docs/blob/main/product/NIGHTCLUB_SETTLEMENT_CASE_STUDY.md) supplies growth context, not an MVP gate or a claim about fabric's private contracts. Launch remains small-event B2B. External ticketing can remain the publication/sales channel; agreeing a contract before listing is distinct from final financial settlement after performance. Keep the fuller scope, questionnaire extensions and acceptance scenarios in those canonical product documents.

## Authority is the request-scoped active tenant, never a token claim

Tokens are identity-only (`sub` + `email`); authority is the active tenant (`X-Tenant-Id` → membership `TenantRole`) resolved per request via `ITenantContext`. Never add a role/authority claim to a B2B token. The tenant *is* the legal/VAT/Stripe entity (`TenantEntity.TaxCompliance`).

The active tenant is therefore the **default scope of every application service**, and names must not restate a default. A controller neither accepts nor resolves a tenant id, and a repository never independently interprets the active request. Name the ordinary scoped use case for its domain intent (`GetDetailsAsync`, `CreateAsync`); name an *alternative* capability explicitly (`GetDetailsByIdAsync`). Repository queries state the actual key (`GetDetailsByTenantIdAsync(Guid tenantId, …)`). Don't add `ActiveTenant` to ordinary method names, and don't add profile-ID resolvers that turn the active tenant into an Artist or Venue id — tenant-owned queries use `TenantId` directly, backed by a module-local projection when they cross a module boundary. `CurrentUser`, `ForUser`, `Me` and `Self` stay reserved for data belonging to the authenticated human.

## Tenant is the canonical backend term

In the current B2B model, one tenant is one business account, legal entity, membership boundary, and settlement identity. There is no separate organisation aggregate or identifier. Use `Tenant` throughout Domain, Application, Infrastructure, and cross-module Contracts. `Organisation` is presentation vocabulary only, such as HTTP DTOs, routes, and UI copy.

Artist and Venue are tenant-owned marketplace profiles, not alternative organisation identities. Do not add `Org*` domain types or cross-module identity lookups. Introduce a separate organisation concept only after an explicit lifecycle or cardinality distinction is established.

## VAT/settlement posture is agent, not principal

VAT/invoice direction branches on contract type **and** the supplier's VAT-registration status. VenueHire reverses supply direction — the artist is the buyer there — so a blanket "add 20% to the artist payout" is wrong. Detail → `LEGAL_REQUIREMENTS.md`.

## Deal ≠ Contract

Deal = the editable economic offer (Deal module, keyed by `DealType`); `ContractEntity` = the frozen snapshot minted at Accept (Booking module). Keep `DealType` variation in the keyed resolver / workflow capability, never a branch in agnostic code (→ `keyed-strategies` skill, families rostered in [`CODE_PATTERNS.md`](./CODE_PATTERNS.md)).

<!-- BEGIN personal:rules sha256:eee7a719bb90e67aaa81ac101a45836dd099bc745fee61216c99ec7d6075f12d -->
# Personal rules

These rules govern every project that explicitly adopts `personal@tj-agents`. Adoption records the
project's personal scope; a home directory, author name or employer association never establishes it, and
employer projects stay outside this profile unless the user explicitly extends that scope. Before planning
or starting work, confirm the task is clear against each rule below.

## 1. Clean cut

Keep one current supported solution. When replacing behavior, remove the superseded names, aliases,
fallback implementations, adapters, configuration formats, compatibility branches and forwarding or
migration docs. Migrate all known callers, project declarations, installed registrations, generated
artifacts and live runtime entry points in the same cutover, and coordinate upgrades with the actual
collaborating developers. Run each migration once during the cutover and delete it; never ship migration
or cleanup code that runs again later. CI fails a commit that removes or renames a skill while a tracked source
still names its old `<package>:<name>`.

Verify the entry points people actually use on every supported platform, including Windows and Linux
launch paths where supported. Source edits, successful generation, passing unit tests and version commands
alone leave a migration unproven; check installed bindings and fresh runtime behavior. Record any
unverified entry point as incomplete, with its resolver and observable acceptance condition.

```text
Replace old-launch with launch:
1. Migrate the registered shell command, configuration and actual callers.
2. Delete the old entry and regenerate installed settings.
3. Invoke launch from a fresh supported Windows/Linux session and check its actual behavior.
Result: launch --version alone leaves this migration incomplete.
```

A backwards-compatibility exception exists only as an explicit user requirement. Surface it as a
deliberate policy decision, identify the affected supported behavior and reconcile this profile's scope
before implementing it, keeping the exception visible rather than inside a fallback or adapter.

## 2. Proportionate implementation

Solve the demonstrated problem with the smallest suitable implementation. Add abstractions, dependencies,
configuration switches, duplicate paths or process machinery only for a concrete current need. Prefer the
capabilities the project already selects. Verify the behavior and meaningful failure paths in proportion
to the change, with tests that prove behavior rather than restate the implementation.

## 3. Plans

Track work as a GitHub issue or project item. A plan committed under `plans/` is only the detailed
implementation plan behind one such item and links to it; working plans stay machine-local.

This policy is authored once here. A selecting project receives it at SessionStart, and adoption generates
its marked native instruction block from this body and checks it for drift. A selected plugin or an
`always` tier alone is not proof of loading.
<!-- END personal:rules -->
