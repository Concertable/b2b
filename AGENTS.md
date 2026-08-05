# Concertable.B2B

Data service — venue↔artist booking + settlement. Inherits root [`AGENTS.md`](../../AGENTS.md) + [`api/AGENTS.md`](../AGENTS.md) (don't restate). Internal design → [`ARCHITECTURE.md`](./ARCHITECTURE.md); deal/contract/workflow → [`src/Modules/Deal/ARCHITECTURE.md`](./src/Modules/Deal/ARCHITECTURE.md) + [`src/Modules/Concert/AGENTS.md`](./src/Modules/Concert/AGENTS.md); legal/VAT → [`src/Modules/Deal/LEGAL_REQUIREMENTS.md`](./src/Modules/Deal/LEGAL_REQUIREMENTS.md).

## Authority is the request-scoped active tenant, never a token claim

Tokens are identity-only (`sub` + `email`); authority is the active tenant (`X-Tenant-Id` → membership `TenantRole`) resolved per request via `ITenantContext`. Never add a role/authority claim to a B2B token. The tenant *is* the legal/VAT/Stripe entity (`TenantEntity.TaxCompliance`).

## VAT/settlement posture is agent, not principal

VAT/invoice direction branches on contract type **and** the supplier's VAT-registration status. VenueHire reverses supply direction — the artist is the buyer there — so a blanket "add 20% to the artist payout" is wrong. Detail → `LEGAL_REQUIREMENTS.md`.

## Deal ≠ Contract

Deal = the editable economic offer (Deal module, keyed by `DealType`); `ContractEntity` = the frozen snapshot minted at Accept (Concert module). Keep `DealType` variation in the keyed resolver / workflow capability, never a branch in agnostic code (→ [`api/docs/CODE_PATTERNS.md`](../docs/CODE_PATTERNS.md)).
