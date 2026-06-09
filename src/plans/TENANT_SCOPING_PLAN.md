# Tenant Scoping Plan

Purely tenant scoping: introduce the tenant as the entity the operator side revolves around, wire the
row-level isolation, re-key settlement to it — and otherwise leave the app behaving as it does today. No
new users, no role/membership model. Just give the data a tenant boundary it currently lacks.

**Status:** Canonical. Replaces the earlier Finbuckle `TENANT_PLAN.md` and
`api/Concertable.B2B/TENANCY_DESIGN.md` — both deleted in the change that lands this. Working doc, not
an archive.

**Scope:** B2B only. Payment is touched at its edge (re-keying the payout owner) but stays an agnostic
adapter — see §6. Customer/Search are unaffected.

**No new users — that's the boundary.** The app keeps behaving roughly as today; this plan just makes
the data **tenant-scoped** underneath it. Everything that should be tenant-scoped *is*, and resolution
**works** — this is not stubbed. What's deliberately deferred is the **multi-user model**: no new user
types, no membership table, no roles (Owner/Manager/read-only), no auth sweep, no multi-tenant switching,
and not the `MessageEntity` group-inbox decision. Those wait for a later session.

The bit that keeps it functional now: **one tenant per setup** — when a user registers/sets up, a tenant
is created and they are its sole, implicit owner (*not* a new user), and `ITenantContext` resolves that
single tenant from `ICurrentUser.Id`. So there's exactly one user per tenant for now, and the filters
genuinely isolate.

---

## 0. Decision (locked)

**Row-level isolation = plain EF Core 10 *named query filters*. We do NOT use Finbuckle.MultiTenant.**

Why, against this codebase specifically:

- **The product is a SaaS-enabled marketplace, not siloed B2B SaaS.** We sell software to venue
  operators (the SaaS part), and the value is operators transacting *with* artists — bookings and
  settlements that belong to **two tenants at once**. That is *marketplace tenancy*: partial,
  relationship-scoped isolation. Finbuckle's model is one-tenant-owns-each-row (`[MultiTenant]` → a
  single `TenantId` equality filter); it fits the *operator-private* half and **cannot express** the
  two-party half (`operator == me || artist == me`). We'd hand-write those predicates under Finbuckle
  anyway — so it would cover only the easy half while we maintain a second mechanism for the money rows
  that matter most.
- **EF Core 10 named filters remove the only historical reason to reach for a library.** Multiple named
  filters per entity, each toggled independently (`IgnoreQueryFilters(["Tenant"])`), so a future
  `"SoftDelete"` filter (GDPR erasure — `Modules/Contract/LEGAL_REQUIREMENTS.md` item 8) coexists with
  `"Tenant"` and is disabled separately. One mechanism covers single-owner *and* two-party rows.
- **It fits the seams we already have.** `ICurrentUser` already resolves identity from JWT claims;
  `AuditInterceptor` already stamps `IAuditable` at `SaveChanges`; per-module `DbContextBase` +
  `XConfigurationProvider` is the natural filter site. Nothing new at the architecture level.
- **One shared DB, one resolution source.** One `B2BDb`, tenant from one `tenant` claim. No per-tenant
  connection strings, no subdomain/header strategies, no tenant store — none of Finbuckle's distinctive
  machinery applies. (Finbuckle becomes appropriate only if we pivot to a *siloed* product with
  per-tenant databases. If that ever happens it's a cheap bolt-on behind `ITenantContext`, because the
  data model below is identical either way.)
- **Keeps Payment agnostic.** Finbuckle would push tenant-resolution middleware into Payment (a shared
  adapter used by B2B *and* Customer). Plain filters let Payment carry an opaque `TenantId` owner key
  with zero library coupling.

**Key type:** `Guid` — matches `UserId`/`CreatedByUserId` across the codebase; opaque & non-enumerable.
Not `string` (that was a Finbuckle convention, and Finbuckle is out).

---

## 1. Vocabulary (locked)

Layer-split naming — the GitHub/Auth0/Linear/WorkOS pattern. "Tenant" is the code/DB word (consistent,
signals the isolation boundary); "Organization" is purely the user-facing word.

| Concern | Name |
|---|---|
| The tenant row — legal/VAT/Stripe identity; owns venues; the settlement payee | **`TenantEntity`** (PK `Guid Id`), schema `tenant`, module `Concertable.B2B.Tenant.*` |
| Single-owner isolation key + marker | `Guid TenantId` + `ITenantScoped { Guid TenantId { get; } }` |
| Two-party isolation keys | `Guid OperatorTenantId` + `Guid ArtistTenantId` (no single-key marker) |
| Ambient "current tenant" accessor | `ITenantContext` — `Guid? TenantId` + derived `HasTenant` + claim-derived `IsHost`; mirrors `ICurrentUser`, grows later |
| Audit marker (exists) | `IAuditable` |
| Controller / route | `TenantController` serving `[Route("organizations")]` |
| UI copy | "Organization", "Org settings" (operators) / "Business & tax details" (artists) |

There is **no separate tenant-info / resolution DTO** (that was Finbuckle's `ITenantInfo`). The tenant
is just the `TenantEntity` row + the `Guid TenantId` + the `ITenantContext` accessor. Nothing reads an
entity to enforce isolation — the filter compares `row.TenantId == TenantContext.TenantId`.

`TenantEntity` is the existing scaffolded `OrganizationEntity`, renamed and grown. It is the tenant
**root**: keyed by its own `Guid Id`, does **not** implement `ITenantScoped`, does **not** carry a
`TenantId`. Per `MODULAR_MONOLITH_RULES`, every `TenantId` FK is a **plain `Guid` property, never an EF
navigation across module boundaries**.

### Why a `TenantEntity` exists here (some tenant systems have none)

A pure **database-per-tenant** app (each tenant = its own DB; the tenant list in a `ConfigurationStore`
/ Azure App Config) has **no tenant table** — there the tenant is *infrastructure metadata* ("which
connection string?"), so there's nothing to map a table to. Ours is the opposite: the tenant is a
**business/legal entity** whose data (VAT number, Stripe account, registered address, members,
per-tenant config) is user-entered, edited at runtime, and queried for DAC7/settlement/invoicing. That
must live in a queryable table — you can't put a venue's VAT number in `appsettings`. We are shared-DB
row-scoped, deliberately, since per-tenant databases are overkill at our scale.

---

## 2. Entity classification — the investigation result

Three buckets. The dividing question is **whose private management surface does this row belong to** —
*not* "is it ever visible." Public marketplace browse is served by Customer/Search projections, **not**
B2B's tenant-scoped contexts, so scoping B2B's *management* reads does not hide marketplace data.

### Bucket A — operator-private, single-owner → `ITenantScoped` + `Guid TenantId`, equality filter

`HasQueryFilter("Tenant", e => ctx.IsHost || e.TenantId == ctx.TenantId)`

| Entity | Module | Owner key today | Change |
|---|---|---|---|
| `VenueEntity` | Venue | `Guid UserId` (creating manager) | add `Guid TenantId`; keep `UserId` as creator/audit |
| `VenueImageEntity` | Venue | child of Venue | carry `TenantId` for filter simplicity |
| `OpportunityEntity` | Concert | `int VenueId` | add `Guid TenantId` (= the venue's operator tenant) |
| `ContractEntity` (TPH) | Contract | none (ref'd by `Opportunity.ContractId`) | operator's terms template — add `Guid TenantId` |

### Bucket B — two-party shared → `Guid OperatorTenantId` + `Guid ArtistTenantId`, OR-filter

`HasQueryFilter("Tenant", e => ctx.IsHost || e.OperatorTenantId == ctx.TenantId || e.ArtistTenantId == ctx.TenantId)`

No single-key `ITenantScoped`; the filter is configured **explicitly** in the module's EF config. Both
ids are **denormalized/snapshotted** (§4) — consistent with how `ConcertEntity` already denormalizes
`ArtistReadModel`/`VenueReadModel`, and with the snapshot-at-Accept the legal work needs anyway
(`LEGAL_REQUIREMENTS.md` items 2 & 9).

| Entity | Module | Two-party keys today | Change |
|---|---|---|---|
| `ApplicationEntity` (TPH) | Concert | `ArtistId` + `Opportunity.VenueId` | denormalize both tenant ids at apply |
| `BookingEntity` (TPH) | Concert | via `Application` | inherit both at accept |
| `ConcertEntity` | Concert | `ArtistId` + `VenueId` | denormalize both at draft/accept |
| `ConcertImageEntity` | Concert | child of Concert | scope via Concert |

### Bucket C — cross-tenant / unfiltered (no filter)

Marketplace supply, reference data, identity, infra. Filtering any of these breaks discovery,
projections, or messaging.

| Entity | Module | Why unfiltered |
|---|---|---|
| `ArtistEntity` | Artist | **supply side** — discoverable across all operators. Gets its **own** `Guid TenantId` (the artist's sole-trader legal entity, for VAT/payout) but is **not** filtered. |
| `ArtistReadModel`, `VenueReadModel` | Concert | event-populated denormalized reference; read in the context of already-filtered parents |
| `ArtistRatingProjection`, `VenueRatingProjection`, `ConcertRatingProjection` | Artist/Venue/Concert | public aggregate marketplace data |
| `ArtistReview`, `VenueReview` | Artist/Venue | marketplace data |
| `UserEntity` | User | identity projection (written by `CredentialRegisteredHandler`); user→tenant link is **membership** (later user-model plan), not a row filter |
| `MessageEntity` | Conversations | **provisional** — participant-scoped (`FromUserId == me \|\| ToUserId == me`) today; leaning **tenant-scoped (Bucket B), a group inbox**, deferred to a later user-model plan (see §8). |
| Outbox / Inbox | messaging infra | never scoped |

---

## 3. The mechanism

### 3.1 `ITenantContext` — resolve the current tenant

Interface in `Concertable.Kernel.Identity` next to `ICurrentUser`. For the one-tenant-per-user world, the
B2B implementation resolves the current user's single tenant from `ICurrentUser.Id` via a request-scoped
memoized lookup (the `IContractAccessor` pattern) — no Auth change needed. (A `tenant` *claim* carrying an
*active* tenant is only needed once a user can belong to several tenants and switch — that's the deferred
multi-user work.)

```csharp
public interface ITenantContext
{
    Guid? TenantId { get; }              // current tenant; null = host (no tenant resolved)
    bool HasTenant => TenantId.HasValue; // derived sugar — for guard clauses
    bool IsHost { get; }                 // claim-derived: platform admin or system/service caller — the filter bypass
}
```

**`IsHost` is load-bearing for correctness, and is deliberately NOT just `!HasTenant`.** Event handlers,
workers, the outbox dispatcher, and projection updaters run with **no user → no tenant claim**. If they
ran filtered, `TenantId` would be null and every tenant-scoped query would read/write nothing — silently
breaking projections and settlement, which legitimately cross tenants. So the system/service/worker
principal (no end-user) resolves `IsHost = true` — in scope and needed for those jobs to run. (A
`platform`-admin *human* bypass is a later refinement; admins can use `IgnoreQueryFilters(["Tenant"])`
explicitly until then.)

Why a separate `IsHost` rather than "null tenant ⇒ host": a bug that *drops* the tenant claim off a
normal operator would make `HasTenant == false`. If that alone bypassed the filter, the operator would
read **every** tenant's data. With an explicit claim-derived `IsHost`, a dropped claim yields
`HasTenant == false` **and** `IsHost == false` → the query matches **nothing** (safe failure). `HasTenant`
is only convenience sugar for guard clauses (`if (!ctx.HasTenant) throw …`); it never drives the bypass.

### 3.2 Applying the filters

`ITenantScoped` lives in `Concertable.Kernel` next to `IAuditable` (a bare marker imposes nothing on
services that don't use it). A B2B infrastructure helper applies the single-owner filter to **every**
`ITenantScoped` type, called from each B2B module context's `OnModelCreating` after
`provider.Configure(modelBuilder)`:

```csharp
modelBuilder.ApplyTenantScoping(tenantContext);  // loops entity types implementing ITenantScoped,
                                                  // adds HasQueryFilter("Tenant", e => ctx.IsHost || e.TenantId == ctx.TenantId)
```

`ITenantContext` is injected into each B2B `DbContext` ctor (exactly like the existing
`XConfigurationProvider`). The filter lambda references the context instance member, so EF re-evaluates
it per query.

**Do NOT put this in the shared `DbContextBase`** — that type is cross-service (Customer/Search use it).
The helper + per-context call keeps tenancy a B2B concern.

Two-party (Bucket B) entities get their OR-filter **explicitly** in their module's
`IEntityTypeConfiguration`. The filter name `"Tenant"` is reserved per entity; a future `"SoftDelete"`
filter is a *separate* named filter, toggled independently.

### 3.3 Write-side guard — `TenantInterceptor`

Plain filters protect *reads*; we add a `SaveChangesInterceptor` (modeled on `AuditInterceptor`, same
project) that, on `Added` `ITenantScoped` entities, stamps `TenantId = ctx.TenantId` when unset and
**throws** on a cross-tenant write (`TenantId != ctx.TenantId` for a non-host principal). This recovers
the one ergonomic Finbuckle gives that filters don't — auto-stamp + mismatch protection — in ~30 lines we
already have the pattern for. Two-party rows are stamped by their workflow at Accept, not the interceptor.

### 3.4 Admin / platform bypass

`IgnoreQueryFilters(["Tenant"])` — explicit, greppable, per-query, leaving any other named filter (e.g.
`"SoftDelete"`) intact. Used by platform-admin endpoints and any cross-tenant job not already running as
`IsHost`.

---

## 4. Two-party security — where the tenant ids come from

The two-party rows hold `VenueId`/`ArtistId` ints with no nav across module boundaries, so the filter
cannot traverse to `Venue.TenantId`. The ids are **denormalized at write time**:

- `OpportunityEntity` (Bucket A) is created by the operator → `TenantId` = the venue's operator tenant.
- `ApplicationEntity` (Bucket B) at apply: `OperatorTenantId` = opportunity's operator tenant;
  `ArtistTenantId` = the applying artist's tenant. Both snapshotted onto the row.
- `BookingEntity` inherits both from its `Application` at accept (`AcceptExecutor`).
- `ConcertEntity` carries both from draft/accept.

This snapshot is the **same** frozen-at-Accept data the legal/audit work requires
(`LEGAL_REQUIREMENTS.md` items 2 & 9). Settlement reads the booking snapshot, never live tenant state —
so the isolation predicate and the audit guarantee reinforce each other.

---

## 5. Stripe Connect settlement lifecycle (re-keyed to tenant)

Today the connected account is **per user** (`PayoutAccountEntity.UserId`), provisioned by
`ManagerRegisteredHandler` on `CredentialRegisteredEvent` (filters venue/artist client ids →
`ProvisionCustomerAsync` + `ProvisionConnectAccountAsync(userId, email)`). Settlement moves money between
users (`PayoutFinishStep.PayAsync(venueUserId, artistUserId, …)`, `EscrowEntity.FromUserId/ToUserId`).

The legal posture (`LEGAL_REQUIREMENTS.md` item 0 — Concertable is an **agent**) requires money to move
via the **parties' own connected accounts**, and the payee is the **legal entity** = the tenant:

- **Provision per tenant, not per user.** `TenantService.CreateAsync` publishes `TenantCreatedEvent`; a
  Payment handler provisions the Connect account on that, keyed by `TenantId`.
  `ManagerRegisteredHandler` stops provisioning Stripe. One Connect account per tenant (operator *and*
  artist tenants each get one — both send/receive).
- **Escrow path** (FlatFee, VenueHire) — `ReleaseEscrowFinishStep → escrowClient.ReleaseByBookingIdAsync`
  transfers held funds to the payee tenant's connected account.
- **Off-session path** (DoorSplit, Versus) — `PayoutFinishStep` reads the booking's frozen party snapshot
  (operator tenant + artist tenant) and pays off-session between their connected accounts.
- Only the **platform fee** is Concertable's revenue; the venue↔artist leg never becomes ours.

---

## 6. Payment service stays agnostic

Payment is a shared adapter (B2B + Customer). It does **not** import `ITenantContext`, named filters, or
any B2B tenancy code. It only swaps its **owner key**:

| Payment entity | Today | After |
|---|---|---|
| `PayoutAccountEntity` | `Guid UserId` (single owner) | `Guid TenantId` — the legal entity's Stripe account |
| `TransactionEntity` (Settlement/Ticket/Verify TPH) | `FromUserId`/`ToUserId` | `From`/`To` carry tenant ids |
| `EscrowEntity` | `FromUserId`/`ToUserId` + `BookingId` | `From`/`To` carry tenant ids |
| `StripeEventEntity` | — | webhook log; infra; not scoped |

To Payment, `TenantId` is an opaque `Guid` owner key — exactly how it treats `UserId` today. B2B resolves
`venue → operatorTenantId` and `artist → artistTenantId` (from the booking snapshot) and passes them
across gRPC. No row-level query filter is added in Payment; it is called with explicit ids, not serving
tenant-scoped browse.

---

## 7. Phases

Each phase ends green (build + tests). Migrations **re-scaffold** via `./initial-migrations.ps1` from
`api/` (per `api/CLAUDE.md` — no additive migrations). No production data, so the nuke is free.

- **Phase 0 — Rename `Organization` → `Tenant`. ✅ Done.** Mechanical: `Concertable.B2B.Organization.*` →
  `Concertable.B2B.Tenant.*`, folder, namespaces, `OrganizationEntity` → `TenantEntity` (PK `int` →
  `Guid`), `OrganizationDbContext` → `TenantDbContext`, `IOrganizationModule` → `ITenantModule`, schema
  `organization` → `tenant`, `AddOrganizationApi/Module` → `AddTenantApi/Module`, `.slnx`, IVT, the
  `initial-migrations.ps1` entry (keeps its 4th slot). Build Web + Workers + tests green.
- **Phase 1 — `ITenantContext` + markers + interceptor. ✅ Done.** `ITenantScoped` (Kernel) +
  `ITenantContext` (Kernel.Identity) + `ApplyTenantScoping` (B2B.DataAccess, EF Core 10 named filter
  `"Tenant"`) + `TenantInterceptor` (DataAccess.Infrastructure, modeled on `AuditInterceptor`).
  `ITenantContext` impl (`TenantContext`, in the Tenant module; scoped, memoized via `ResolveAsync`)
  resolves the user's single tenant from `ICurrentUser.Id` → `TenantEntity.CreatedByUserId`. `IsHost` =
  **no `HttpContext`** (worker/outbox/handler ⇒ bypass); an anonymous HTTP request keeps `IsHost` false,
  so it fails closed. Plumbing only — **not wired into any context yet**; the resolution trigger
  (request middleware calling `ResolveAsync`) + `ApplyTenantScoping`/interceptor wiring land in Phase 2.
  Build + unit tests (tenant / host / anonymous / authenticated-no-tenant) green.
- **Phase 2 — Bucket A scoping.** `TenantId` on `VenueEntity`/`VenueImageEntity`/`OpportunityEntity`/
  `ContractEntity`, implement `ITenantScoped`, wire `ApplyTenantScoping` into the Venue/Concert/Contract
  contexts. `VenueService`/`OpportunityService` create against `ctx.TenantId`. Integration tests:
  cross-tenant read returns nothing; host bypass works.
- **Phase 3 — Re-key payouts + provision on tenant creation.** Payment `PayoutAccountEntity` `UserId` →
  `TenantId`; `TenantCreatedEvent` provisions Stripe; `ManagerRegisteredHandler` stops. Update mocks +
  Payment tests; re-scaffold Payment migration.
- **Phase 4 — Bucket B scoping + snapshot at Accept.** `OperatorTenantId`/`ArtistTenantId` on
  `Application`/`Booking`/`Concert`/`ConcertImage`; populate at apply/accept (`AcceptExecutor`); explicit
  OR-filter in their configs. Settlement steps read the snapshot. Assert snapshot-at-Accept +
  settlement-reads-snapshot + cross-tenant invisibility of bookings.
- **Phase 5 — Compliance value object on `TenantEntity`** (= `LEGAL_REQUIREMENTS.md` item 3): VAT /
  seller identifier / registered address / bank ref as owned value objects. Full round-trip test (nested
  owned types are the main EF risk). Tenant-setup UI hitting `/organizations`.
_(Membership, roles, the authorization sweep, multi-user orgs, and the `MessageEntity` group-inbox
decision are **out of scope** — a later user-model plan owns them. See the scope note at the top.)_

Phases 0–1 are pure plumbing; 2 and 4 turn on the two filter shapes; 3 and 5 are the money/legal
payloads. Each is independently shippable.

---

## 8. Open product decisions

- **Artist tenancy.** Confirmed: `ArtistEntity` gets its **own** `TenantId` (every artist needs a legal
  entity for VAT/payout) but is **exempt from the filter** (Bucket C) — supply must be cross-tenant
  discoverable.
- **Stripe granularity.** One Connect account **per tenant** (operator and artist tenants alike).
- **`MessageEntity` — deferred to the later user-model plan.** Leaning **tenant-scoped** (a group/shared
  inbox: add `FromTenantId`/`ToTenantId` alongside the existing `FromUserId`/`ToUserId`, visible to any
  member of either tenant — Bucket B). It only comes alive with multi-user membership, so the call is
  parked with that work. Sub-points to settle then: per-user vs per-org `Read` state, and keeping
  notification *routing* separate from message *visibility* (tenant-scoping makes a thread visible to the
  org; who gets pinged is the Notification module's job, not the filter's).
- **Solo operator / solo artist.** An independent venue is an operator tenant with one venue; a solo
  artist is a one-person tenant. No special-casing — both are just tenants.

---

## 9. Risks

- **`IsHost` resolution for system/worker contexts.** Get it wrong and projections/settlement silently
  no-op (filtered to a null tenant). Covered by an explicit unit test for the service principal.
- **Two-party snapshot completeness.** A missing `OperatorTenantId`/`ArtistTenantId` at write time hides
  the row from its rightful tenant. Snapshot population is a code-review checklist item at every
  apply/accept site.
- **EF nested owned types** (Phase 5 compliance VO) — round-trip test gates downstream work.
- **Filter coverage.** A new Bucket A entity that forgets `ITenantScoped`, or a Bucket B entity that
  forgets its explicit OR-filter, silently leaks. `ApplyTenantScoping` covers Bucket A automatically;
  Bucket B needs a per-entity config audit.

---

## 10. What this unblocks

- **DAC7 export** — iterate `TenantEntity`, read `Compliance`, emit HMRC schema.
- **Per-contract VAT calc + invoicing** — `LEGAL_REQUIREMENTS.md` items 1 → 4 read VAT off the tenant.
- **Cancellation / escrow refund** — item 6 (needs the `Cancelled` state; separate sub-plan).
- **Per-tenant config** — PRS pass-through, platform fee, payment terms, branding hang off the tenant.

---

## 11. Decision log

- **2026-06-09** — Isolation = **EF Core 10 named query filters**, not Finbuckle. The product is a
  *SaaS-enabled marketplace* (marketplace tenancy): two-party booking/settlement rows Finbuckle's
  one-owner model can't express, plus one shared DB and one claim-based resolution source — so
  Finbuckle's distinctive machinery (per-tenant DBs, resolution strategies, tenant store) is unused while
  `[MultiTenant]` covers only the easy half. Named filters cover both halves with one mechanism; EF Core
  10's independent filter toggling removes the historical "one filter per entity" objection. Reverses the
  2026-06-08 Finbuckle decision.
- **2026-06-09** — Key type **`Guid`** (reverses the Finbuckle-driven `string` choice).
- **2026-06-09** — Naming: code/DB = `TenantEntity`/`TenantId`/`ITenantContext`/`ITenantScoped`; public
  API `/organizations`; UI "Organization". One row (the renamed `OrganizationEntity`), no separate
  tenant-info DTO, no `[MultiTenant]`.
- **2026-06-09** — Ambient accessor is `ITenantContext` (`Guid? TenantId`, derived `HasTenant`,
  claim-derived `IsHost` as the filter bypass). `IsHost` is explicit, not `!HasTenant`, so a dropped
  tenant claim fails safe (sees nothing, not everything). Grows into a memoized gateway to per-tenant
  config later, like `IContractAccessor`.
- **2026-06-09** — Three-bucket entity classification (operator-private / two-party / cross-tenant),
  grounded in the actual entities; system/worker principals resolve `IsHost`.
- **2026-06-09** — Payment stays agnostic: owner key `UserId` → `TenantId` (opaque `Guid`), no B2B
  tenancy code imported.
- **2026-06-09** — **Scope boundary:** in scope = the isolation mechanism, tenant data model, settlement
  re-key, and **one-tenant-per-user resolution** (so the filters actually work and the app stays
  tenant-isolated — functional, not stubbed). Out of scope = the **multi-user model**: new user types,
  membership, roles, the authorization sweep, multi-tenant switching / active-tenant claim, and the
  `MessageEntity` group-inbox. No new users are introduced; exactly one user per tenant for now.
- **2026-06-09** — Supersedes the Finbuckle `TENANT_PLAN.md` and `TENANCY_DESIGN.md` (both deleted).
