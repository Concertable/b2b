# Concertable.B2B

Data service — venue↔artist booking + settlement. Inherits root [`AGENTS.md`](../../AGENTS.md) + [`api/AGENTS.md`](../AGENTS.md) (don't restate). B2B's DbContext stances, filtered-entity list and `DealType` strategy/workflow rosters: @./CODE_PATTERNS.md. Internal design → [`ARCHITECTURE.md`](./ARCHITECTURE.md); deal/contract/workflow → [`src/Modules/Deal/ARCHITECTURE.md`](./src/Modules/Deal/ARCHITECTURE.md) + [`src/Modules/Concert/AGENTS.md`](./src/Modules/Concert/AGENTS.md); legal/VAT → [`src/Modules/Deal/LEGAL_REQUIREMENTS.md`](./src/Modules/Deal/LEGAL_REQUIREMENTS.md).

## Authority is the request-scoped active tenant, never a token claim

Tokens are identity-only (`sub` + `email`); authority is the active tenant (`X-Tenant-Id` → membership `TenantRole`) resolved per request via `ITenantContext`. Never add a role/authority claim to a B2B token. The tenant *is* the legal/VAT/Stripe entity (`TenantEntity.TaxCompliance`).

The active tenant is therefore the **default scope of every application service**, and names must not restate a default. A controller neither accepts nor resolves a tenant id, and a repository never independently interprets the active request. Name the ordinary scoped use case for its domain intent (`GetDetailsAsync`, `CreateAsync`); name an *alternative* capability explicitly (`GetDetailsByIdAsync`). Repository queries state the actual key (`GetDetailsByTenantIdAsync(Guid tenantId, …)`). Don't add `ActiveTenant` to ordinary method names, and don't add profile-ID resolvers that turn the active tenant into an Artist or Venue id — tenant-owned queries use `TenantId` directly, backed by a module-local projection when they cross a module boundary. `CurrentUser`, `ForUser`, `Me` and `Self` stay reserved for data belonging to the authenticated human.

## Tenant is the canonical backend term

In the current B2B model, one tenant is one business account, legal entity, membership boundary, and settlement identity. There is no separate organisation aggregate or identifier. Use `Tenant` throughout Domain, Application, Infrastructure, and cross-module Contracts. `Organisation` is presentation vocabulary only, such as HTTP DTOs, routes, and UI copy.

Artist and Venue are tenant-owned marketplace profiles, not alternative organisation identities. Do not add `Org*` domain types or cross-module identity lookups. Introduce a separate organisation concept only after an explicit lifecycle or cardinality distinction is established.

## VAT/settlement posture is agent, not principal

VAT/invoice direction branches on contract type **and** the supplier's VAT-registration status. VenueHire reverses supply direction — the artist is the buyer there — so a blanket "add 20% to the artist payout" is wrong. Detail → `LEGAL_REQUIREMENTS.md`.

## Deal ≠ Contract

Deal = the editable economic offer (Deal module, keyed by `DealType`); `ContractEntity` = the frozen snapshot minted at Accept (Concert module). Keep `DealType` variation in the keyed resolver / workflow capability, never a branch in agnostic code (→ `keyed-strategies` skill, families rostered in [`CODE_PATTERNS.md`](./CODE_PATTERNS.md)).
