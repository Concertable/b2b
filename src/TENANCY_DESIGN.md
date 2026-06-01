# B2B Multi-Tenancy Design

Status (2026-06-01): **design note, not implemented.** Written when B2B became a separate
service with its own database, making a true B2B-SaaS tenant model viable. Read alongside
[`Modules/Contract/LEGAL_REQUIREMENTS.md`](./Modules/Contract/LEGAL_REQUIREMENTS.md) — the
tenant and the legal/VAT entity are the same thing, so the two designs are joined.

## Premise

Now that B2B owns its own DB and runtime (no shared schema with Customer), we can model
venue groups / operators as **tenants** rather than treating every venue and artist as a
loose, user-owned profile. This matches the product: B2B is a relationship-led SaaS sale to
venue operators (see [`docs/USP.md`](../../docs/USP.md) "Microservices split — strategic"),
and a venue group with five sites is one customer, one bill, one legal entity, five venues.

## The key insight: the tenant IS the legal entity

A tenant here is not just a row-scoping key. Under the **agent / marketplace-facilitator**
posture decided in `LEGAL_REQUIREMENTS.md` item 0, almost every legal attribute hangs off
the tenant, not the individual venue/artist profile:

- VAT registration status + VAT number
- Registered legal name + registered address (invoicing identity)
- The Stripe Connect account used for booking-settlement payouts (and, if the marketplace
  is later added, the merchant of record for that operator's ticket sales)
- Invoice issuer identity + sequential invoice numbering scope
- Subscription / billing relationship with Concertable
- The set of venues (and optionally artists/managers) it owns

So tenancy and the VAT/invoice work in `LEGAL_REQUIREMENTS.md` items 1–3 are the **same
build**: those fields live on the tenant.

## What already exists

`Concertable.B2B.Organization` is a full module slice (Domain / Application / Infrastructure
/ Contracts + tests) with `OrganizationEntity`:

```
Id (int), LegalName (string), CreatedByUserId (Guid), CreatedAt
```

This is the seed of the tenant — `LegalName` is doing nothing useful yet but is exactly the
anchor. **Recommendation: grow `OrganizationEntity` into the tenant** rather than introduce
a parallel `TenantEntity`. (Bikeshed the public name later — "Organization" reads fine
externally; "Tenant" can stay internal vocabulary.)

The gap: **nothing links `VenueEntity` / `ArtistEntity` / manager profiles to an
organisation.** They currently carry only `UserId`. That FK is the first structural change.

## Proposed model

### 1. Promote `OrganizationEntity` to the legal/tenant entity
Add: `VatRegistered` (bool), `VatNumber` (string?), `RegisteredAddress`, `StripeAccountId`
(or a ref to the Payment-side connected account), `BillingStatus` / subscription ref,
`InvoiceSequence` (or a separate numbering service scoped by org).

### 2. Add tenant FK to owned entities
`VenueEntity.OrganizationId` (required) and, depending on product, `ArtistEntity` and
manager profiles. A venue **must** belong to an organisation; an independent solo venue is
just an organisation with one venue. This keeps every venue's legal/VAT/Stripe identity
resolvable via its org.

Cross-module note: per [`docs/MODULAR_MONOLITH_RULES.md`], `OrganizationId` on
`VenueEntity` is a **plain int property**, not an EF navigation across module boundaries
(same rule as existing cross-module FKs). Venue resolves org data via `IOrganizationModule`.

### 3. Tenant scoping (row-level isolation)
Two-layer approach:
- **Ambient tenant context** — resolve the caller's organisation from their auth claims
  into a scoped `ITenantContext` (organisation id). Auth already routes by OAuth client_id
  and is role-agnostic, so add an `org` claim at registration / on the credential.
- **Query filtering** — EF Core global query filters on tenant-scoped entities
  (`HasQueryFilter(e => e.OrganizationId == tenantContext.OrganizationId)`), so a venue
  manager physically cannot read another operator's bookings even via a bug. Admin/platform
  context bypasses the filter explicitly.

This is **shared-database, shared-schema, row-scoped** tenancy — appropriate now. The DB is
already physically separate from Customer; per-tenant DB isolation is not needed at this
scale and would fight the existing module/migration setup.

### 4. Provisioning flow
A tenant is created when an operator signs up. Reuse the event-driven pattern: tenant
creation (or its first manager's `CredentialRegisteredEvent`) provisions the Stripe Connect
account (Payment already does this per-user via `CredentialRegisteredHandler` — extend to
per-organisation, since the **org** is the legal entity that receives settlement payouts —
and, later, is the ticket-sale merchant of record — not the individual user).

### 5. Tenant configuration (high level)

Several behaviours are not platform-global — they vary per operator and so belong on the
tenant as a **configuration surface** (a settings bag / typed config owned by the
organisation), not as hardcoded constants. PRS is the motivating example, but it's one of
many:

- **PRS handling** — is this venue self-licensed for PRS (don't deduct), or does it want a
  pass-through, and at what rate? (See `Modules/Contract/LEGAL_REQUIREMENTS.md` item 5 — the
  rate must come from tenant config, never a literal.)
- **VAT** — registered status + number (the identity fields in §1; conceptually the same
  config surface).
- **Platform fee / commission** — the rate Concertable charges this tenant (may differ by
  plan or negotiation).
- **Default payment terms** — e.g. net-14 (GigPig-style), per-tenant default applied to
  generated invoices.
- **Default contract / cancellation terms** — non-refundable-deposit policy, default
  cancellation window (feeds the booking agreement, item 2, and cancellation, item 6).
- **Branding / invoice issuer identity** — legal name, address, logo on agreements/invoices.

Design intent only: a tenant owns its config; settlement, invoicing, agreements, and PRS all
**read** from it rather than embedding constants. Whether this is one `OrganizationConfig`
row, a key-value settings table, or typed columns is an implementation choice for later — the
point here is that the *need* for per-tenant config is real and PRS is not a special case.

## Open questions (decide before building)

- **Artist tenancy.** Venues clearly belong to an operator tenant. Do *artists* (and their
  managers) also sit under organisations, or are they cross-tenant participants who apply
  into any operator's opportunities? Leaning: artists are **not** tenant-scoped the same way
  — they're a shared supply side that transacts *with* tenants. Their VAT identity still
  needs to live somewhere (an artist-side legal entity), but they shouldn't be row-isolated
  out of the marketplace. Needs a product call.
- **Stripe account granularity.** One Stripe connected account per organisation, or per
  venue? Per-organisation is simpler and matches "one legal entity"; per-venue may be needed
  if sites are separate legal entities. Default: per-organisation, allow override. (Applies
  to settlement payouts now; to ticket-sale merchant-of-record later.)
- **Migration of existing data.** Existing user-owned venues need backfilling into a
  default single-venue organisation each.

## Sequencing

1. Settle the two open questions (artist tenancy, MoR granularity).
2. Grow `OrganizationEntity` with legal fields (this is also `LEGAL_REQUIREMENTS.md` item 1).
3. Add `OrganizationId` FK to `VenueEntity` (+ backfill).
4. `ITenantContext` + `org` auth claim + EF global query filters.
5. Per-organisation Stripe Connect provisioning.
6. VAT/invoice work (`LEGAL_REQUIREMENTS.md` items 2–3) now that the legal entity exists.

Re: migrations — per [`api/CLAUDE.md`], model changes here are **not** additive migrations;
re-scaffold via `./initial-migrations.ps1` from `api/`.
