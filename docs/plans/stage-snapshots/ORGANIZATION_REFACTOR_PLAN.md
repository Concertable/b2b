# Organization Refactor Plan

> **Status:** Draft, ready for review. Phases are independently shippable — each ends with a green build and passing tests.
>
> **Companion docs:** [B2B_LAUNCH_CHECKLIST.md](B2B_LAUNCH_CHECKLIST.md), [MODULAR_MONOLITH_RULES.md](../api/docs/MODULAR_MONOLITH_RULES.md), [../api/Concertable.B2B/Modules/Contract/LEGAL_REQUIREMENTS.md](../api/Concertable.B2B/Modules/Contract/LEGAL_REQUIREMENTS.md).

---

## 1. Goal

Introduce `OrganizationEntity` as the legal-entity carrier for compliance, Stripe Connect, and (eventually) multi-user membership. `VenueEntity` and `ArtistEntity` become **profiles** of an Organization (1:1 for now). Snapshot the Organization's compliance state onto `BookingEntity` at the moment the booking becomes binding, via a `ComplianceContext` owned-type value object.

This unblocks: DAC7 reporting, VAT-correct settlement, audit-grade booking history, and a clean future path to multi-staff venues / bands with managers without re-shaping the data model.

## 2. Scope

**In scope:**
- New `Organization` module under `api/Modules/Organization/`.
- `OrganizationId` FK on `VenueEntity` and `ArtistEntity` (non-unique FK — one Org can have at most one Venue profile and at most one Artist profile, enforced at service-layer rather than DB-constraint, so the model supports relaxation later without re-migration).
- `PayoutAccountEntity` re-keyed from `UserId` to `OrganizationId`.
- `ComplianceContext` value object — snapshotted onto `BookingEntity` at Accept.
- Compliance onboarding flow: new "Organization setup" step after venue/artist creation.
- **Multi-user membership table** (`OrganizationMembership`) — done as part of this refactor (Phase 6) so the auth model is correct from day one. Even if every org launches with one Owner-role member, the table exists and auth checks go through it. Avoids a second refactor across all auth touch-points later.
- Migration order: `Organization` inserts at position 2.5 (after User, before Artist/Venue); `OrganizationMembership` lives in the same module.
- Test updates across affected modules.

**Genuinely out of scope (separate work, not deferred architecture):**
- DAC7 annual XML export script — a feature on top of this refactor that runs once a year. Planned in `LAUNCH_PLAN.md` Swim-lane C.
- Org-switcher UI (a user actively managing multiple orgs) — UX feature; model already supports it post-refactor.
- "Invite teammate" UI — UX feature; model already supports it post-refactor once Phase 6 lands.
- Customer marketplace consumer-protection work — see `MARKETPLACE_PLAN.md`.
- Row-level security / true tenant isolation (we're not building that — see "Naming" below).

## 3. Naming + spelling

- **`Organization`** (US spelling) — matches codebase convention (`Authorization`, etc.).
- **Not "Tenant"** — we're not implementing technical tenant isolation. "Organization" is a domain entity, not an infrastructure boundary.
- `IOrganizationModule` for the cross-module facade. `OrganizationEntity`, `OrganizationDbContext`, `OrganizationService`, etc.

## 4. Target architecture

### 4.1 Entity model

```
OrganizationEntity (NEW — legal entity)
├── int Id
├── string LegalName            (e.g. "The Dog & Duck Ltd")
├── string? TradingName         (optional — e.g. "The Dog & Duck")
├── ComplianceContext Compliance  (owned type)
├── (creator) Guid CreatedByUserId

  ComplianceContext (owned value object)
  ├── string TaxResidence       (ISO country, default "GB")
  ├── bool VatRegistered
  ├── string? Vrn               (null if not VAT registered)
  ├── SellerIdentifier Identifier  (discriminated)
  ├── Address RegisteredAddress (re-uses existing Shared.Domain.Address)
  ├── BankAccountReference Bank (owned — sort code + last4 only; full number lives in Stripe)

  SellerIdentifier (sum type — exactly one is set)
  ├── UtrIdentifier(Utr)              — sole trader
  ├── CompanyIdentifier(CompanyNumber, Utr)  — ltd company
  └── NinoIdentifier(Nino)            — individual artist sole trader

VenueEntity (existing — gains FK)
├── int Id, …existing fields…
└── int OrganizationId (NEW, FK to OrganizationEntity, 1:1 unique index)

ArtistEntity (existing — gains FK)
├── int Id, …existing fields…
└── int OrganizationId (NEW, FK to OrganizationEntity, 1:1 unique index)

PayoutAccountEntity (existing — re-keyed)
├── int Id
├── int OrganizationId (NEW, replaces UserId as primary scope)
├── Guid? CreatedByUserId  (kept for audit only — nullable, optional)
├── string? StripeAccountId, StripeCustomerId, Status   …unchanged

BookingEntity (existing — gains snapshot)
├── …existing fields…
├── ComplianceContext VenueCompliance     (NEW — owned, snapshot at Accept)
├── ComplianceContext ArtistCompliance    (NEW — owned, snapshot at Accept)
└── PlatformFeeSnapshot PlatformFee       (NEW — owned, captures VAT rate + fee % at Accept)
```

### 4.2 Module layout

```
api/Modules/Organization/
├── Concertable.Organization.Domain/
│   ├── OrganizationEntity.cs
│   ├── ComplianceContext.cs              (also re-exported via Shared if needed)
│   ├── SellerIdentifier.cs
│   ├── BankAccountReference.cs
│   └── Events/OrganizationCreatedDomainEvent.cs
├── Concertable.Organization.Contracts/
│   ├── IOrganizationModule.cs            (cross-module facade)
│   ├── OrganizationDto.cs
│   ├── ComplianceContextDto.cs
│   └── Events/OrganizationCreatedEvent.cs
├── Concertable.Organization.Application/
│   ├── Services/IOrganizationService.cs + OrganizationService.cs   (internal)
│   ├── Repositories/IOrganizationRepository.cs                     (internal)
│   ├── Validators/OrganizationValidators.cs
│   ├── Requests/CreateOrganizationRequest.cs, UpdateComplianceRequest.cs
│   └── DTOs/                              (internal)
├── Concertable.Organization.Infrastructure/
│   ├── Data/OrganizationDbContext.cs
│   ├── Data/Configurations/OrganizationEntityConfiguration.cs
│   ├── Repositories/OrganizationRepository.cs
│   ├── OrganizationModule.cs              (impl of IOrganizationModule)
│   └── Extensions/ServiceCollectionExtensions.cs
├── Concertable.Organization.Api/
│   └── Controllers/OrganizationController.cs
└── Tests/
    ├── Concertable.Organization.UnitTests/
    └── Concertable.Organization.IntegrationTests/
```

### 4.3 Compliance snapshot pattern

`ComplianceContext` is an **owned-type value object**, following the existing precedent set by `Shared.Domain.Address` and `Concert.Domain.DateRange`. EF configuration uses `OwnsOne` with custom column naming.

Snapshot is taken **at Accept** (when `ApplicationEntity` transitions to `BookingEntity`), not at Opportunity creation. Rationale: the Application is speculative; the Booking is binding. At settlement time, workflow steps read `booking.VenueCompliance` and `booking.ArtistCompliance`, **never** the current state of `OrganizationEntity`.

Implementation note: when EF Core owns a value object that itself owns another value object (e.g. `ComplianceContext.RegisteredAddress`), nesting works but requires explicit `OwnsOne` on the inner level. Pattern is well-trodden.

## 5. Phased implementation

Each phase is independently shippable.

### Phase 0 — Scaffolding (the new module, no behaviour yet)

**Goal:** create the Organization module skeleton, register DI, run `initial-migrations.ps1` to scaffold the new context. No production code change yet.

**Steps:**

1. Create solution structure: `api/Modules/Organization/` with the 5 standard projects (`Domain`, `Contracts`, `Application`, `Infrastructure`, `Api`) plus two test projects. Mirror an existing module (e.g. Venue) for file layout, `.csproj` targets, IVT, etc.
2. Add `OrganizationEntity` as a minimal aggregate: `Id`, `LegalName`, `CreatedByUserId`, `CreatedAt`. No compliance fields yet.
3. Add empty `IOrganizationModule` facade in `Contracts` with one method: `Task<OrganizationDto?> GetByIdAsync(int id, CancellationToken ct)`.
4. Add `OrganizationDbContext` with one `DbSet<OrganizationEntity>`.
5. Wire `AddOrganizationModule()` extension into Web + Workers composition roots.
6. Insert Organization into `api/initial-migrations.ps1` at position 2.5 (after User, before Artist/Venue).
7. Run `./initial-migrations.ps1` from `api/`. Verify `OrganizationDbContext` has its `InitialCreate` migration.
8. Create empty test projects with a single smoke test each.
9. Build green, all tests pass.

**Files created (~15):** module skeleton + InitialCreate migration + DI registration.
**Files changed (~3):** `Concertable.Web/Program.cs`, `Concertable.Workers/Program.cs`, `api/initial-migrations.ps1`.

### Phase 1 — Compliance fields + ComplianceContext value object

**Goal:** `OrganizationEntity` carries compliance, `ComplianceContext` value object is defined and reusable.

**Steps:**

1. Define `ComplianceContext` record in `Organization.Domain`. Properties per §4.1. Constructor validates (throws `DomainException` on invalid VRN format, missing required identifier, etc.) — match `Address.cs` validation style.
2. Define `SellerIdentifier` abstract record + three concrete records (`UtrIdentifier`, `CompanyIdentifier`, `NinoIdentifier`). Use discriminator column in EF config.
3. Define `BankAccountReference` record (sort code + last4). Full account number stays in Stripe.
4. Add `ComplianceContext Compliance` property on `OrganizationEntity`. Mark nullable initially (organizations created in Phase 0 didn't have it) — flip to required in Phase 2 once backfill is done.
5. EF configuration: `OwnsOne(o => o.Compliance, ...)` with nested `OwnsOne` for `RegisteredAddress` and `BankAccountReference`. Discriminator column for `SellerIdentifier`.
6. Add `IOrganizationModule.UpdateComplianceAsync(...)` method + service impl.
7. Re-scaffold `OrganizationDbContext` migration to include compliance columns.
8. Unit tests for `ComplianceContext` validation (each invalid input → throws), `SellerIdentifier` discriminator round-trip, EF persistence round-trip.

**Risk:** EF Core nested owned types. Test the persistence round-trip end-to-end before declaring done.

### Phase 2 — Wire Venue and Artist to Organization

**Goal:** every Venue and every Artist has an `OrganizationId`. Existing code paths still work.

**Steps:**

1. Add `int OrganizationId` to `VenueEntity` and `ArtistEntity`. Add a **non-unique** index in EF config (for query performance). Uniqueness — i.e. one Org has at most one Venue / one Artist — is enforced at service-layer (`VenueService.CreateAsync` rejects if the Org already has a Venue), not at DB constraint level. Reason: keeps the DB shape forward-compatible for the future case where one Org acts as both Venue *and* Artist, without a second migration.
2. Add factory methods: `VenueEntity.Create(..., organizationId)` requires the FK. Same for `ArtistEntity`.
3. Update `VenueService.CreateAsync` and `ArtistService.CreateAsync` to:
   - First call `IOrganizationModule.CreateAsync(legalName, createdByUserId)` to provision the Org
   - Then create the Venue/Artist with the returned `OrganizationId`
   - Wrap in a single `IUnitOfWorkBehavior` so failure rolls back both
4. Update the two `CreateVenueRequest` / `CreateArtistRequest` DTOs **only** if we want to capture LegalName at signup. Decision: **no for MVP** — derive `LegalName = venue.Name` initially, let user update later via Org settings page.
5. Re-scaffold Venue, Artist, Organization migrations.
6. Update Venue + Artist integration tests to expect an Organization is also created. Add a test for the rollback case (Org creation fails → Venue/Artist not persisted).

**Migration risk:** none for fresh databases. Production data migration is N/A because there's no production data yet (proof-of-concept stage, confirmed).

### Phase 3 — Re-key PayoutAccountEntity from UserId to OrganizationId

**Goal:** the Stripe Connect account belongs to the legal entity, not a user.

**Steps:**

1. Add `int OrganizationId` to `PayoutAccountEntity`. Keep `Guid? CreatedByUserId` as audit nullable.
2. Update `IPayoutAccountRepository` — replace `GetByUserIdAsync` with `GetByOrganizationIdAsync`.
3. Change `UserRegisteredHandler.cs` — **stop** provisioning Stripe on User registration. Instead, subscribe to `OrganizationCreatedEvent` (new integration event published from `OrganizationService.CreateAsync`) and provision on that.
4. Update `StripeAccountClient.ProvisionConnectAccountAsync(...)` signature: accept `OrganizationId` + email instead of `UserId` + email. Stripe doesn't care which is which — just metadata.
5. Update `StripeAccountController.GetOnboardingLink()` and `GetAccountStatus()`: resolve current user → their venue/artist → its organizationId → PayoutAccount.
6. Update mocks: `MockStripeAccountClient`, `MockStripeAccountClientFail` to accept OrganizationId.
7. Update Payment unit + integration tests.
8. Re-scaffold Payment migration.

**Risk:** the chain `currentUser → venue/artist → organization` resolution needs to be efficient. Consider an `IOrganizationLookup` request-scoped memoizer following the existing `IContractLoader` pattern.

### Phase 4 — Snapshot ComplianceContext onto BookingEntity

**Goal:** at Accept, freeze the venue + artist compliance state onto the Booking. Settlement reads the snapshot.

**Steps:**

1. Add `ComplianceContext VenueCompliance` and `ComplianceContext ArtistCompliance` to `BookingEntity`. Both as owned types in EF config.
2. Add `PlatformFeeSnapshot PlatformFee` (VAT rate, fee %, currency) — owned. Sourced from a new `PlatformFeeService` reading from appsettings.json initially (DB-backed versioning is Phase 5+ work).
3. In `AcceptExecutor` (or wherever Application → Booking transition happens), populate the snapshots **before** SaveChanges. Use `IOrganizationModule.GetByIdAsync` (or memoized loader) to read current state, snapshot it.
4. Update finish/settle steps to read from the booking snapshot rather than re-querying organization state. Specifically: `DoorSplitFinishStep`, `VersusFinishStep`, `FlatFeeFinishStep`, `VenueHireFinishStep`.
5. Re-scaffold Concert migration.
6. Update Concert integration tests — assert snapshot is populated at Accept, assert settlement reads snapshot not current state.

**Risk:** snapshot must include *every* field needed by settlement and DAC7. If we miss one, we re-read current state and lose the audit guarantee. Code review checklist item.

### Phase 5 — Organization setup UI (post-signup compliance form)

**Goal:** users can enter their legal/compliance data via a dedicated form. Without this, Phase 4's snapshots are empty.

**Steps:**

1. New page in `app/web/venue/src/features/venue/pages/OrganizationSettingsPage.tsx` (and equivalent in artist SPA).
2. Route: `/organization/settings`. Surface in nav once Venue/Artist is created.
3. Form fields: LegalName, TradingName, TaxResidence, VatRegistered (boolean), Vrn (conditional), Seller identifier (radio: Sole trader UTR / Company / Sole trader NINO + the relevant ID field), RegisteredAddress, Bank sort code + last4 (display Stripe-confirmed details if onboarding done).
4. PUT to new `OrganizationController.UpdateComplianceAsync` endpoint.
5. Validation: mirror `ComplianceContext` constructor rules client-side.
6. Block at posting opportunity / accepting booking if compliance is incomplete. Decision: **soft block initially** (warning banner, link to settings page); **hard block on first settlement** (escrow release fails if compliance missing).
7. E2E test for the happy-path setup flow.

**Risk:** UX. Don't force this immediately on signup — let users browse first, capture later. Worth a design pass with whoever's doing the venue dashboard work (Phase B of `MANAGER_FRONT_PAGE_PLAN.md`).

### Phase 6 — Multi-user membership

**Goal:** auth model flips from "user IS the venue/artist manager via TPH discriminator" to "user has Role X in Organization Y". Auth checks go through membership table. Even at MVP every org has exactly one Owner-role member; the table exists from day one so the second refactor is never needed.

**Steps:**

1. Add `OrganizationMembership` entity in `Organization.Domain`:
   ```
   OrganizationMembership
   ├── Guid UserId            (FK to UserEntity, plain int — no cross-context nav)
   ├── int OrganizationId     (FK to OrganizationEntity)
   ├── MembershipRole Role    (enum: Owner, Manager — extend later)
   ├── DateTime JoinedAt
   └── (UserId, OrganizationId) composite primary key
   ```
2. Define `MembershipRole` enum in `Organization.Contracts`. Two roles for MVP: `Owner` (full access including billing/delete) and `Manager` (operational, no billing/delete). Add `Staff`, `Finance`, etc. when a customer asks.
3. Migrate creation flow: when `OrganizationService.CreateAsync` runs (from Phase 2), also insert a `OrganizationMembership(createdByUserId, newOrgId, Owner)` row in the same transaction.
4. Extend `IOrganizationModule` with:
   - `Task<bool> HasRoleAsync(Guid userId, int organizationId, MembershipRole minRole, CancellationToken ct)`
   - `Task<IReadOnlyList<MembershipDto>> GetMembershipsForUserAsync(Guid userId, CancellationToken ct)`
   - (Future) `AddMemberAsync`, `RemoveMemberAsync`, `ChangeRoleAsync`.
5. Add `ICurrentUserResolver` extension that surfaces the current user's memberships in a request-scoped cache: `IReadOnlyList<MembershipDto> Memberships { get; }`. Existing `ICurrentUser` keeps `UserId` and `Role` (the IDP-emitted role claim).
6. **Auth refactor sweep** — replace existing patterns:
   - `[Authorize(Roles = "VenueManager")]` on controller actions: keep as the *coarse* gate (only VenueManagers can hit venue endpoints), then add a fine-grained policy check inside the action body: `await orgModule.HasRoleAsync(currentUser.Id, venue.OrganizationId, MembershipRole.Manager)`.
   - Service-layer `if (venue.UserId != currentUser.GetId()) throw` patterns: replace with `if (!await currentUser.HasMembershipIn(venue.OrganizationId, MembershipRole.Manager)) throw`.
   - Affected files (from investigation): ~15-25 controller actions across Venue, Artist, Concert, Payment, Customer. ~10-15 service-layer FK checks.
7. Retire `VenueManagerEntity.VenueId` and `ArtistManagerEntity.ArtistId` direct FKs. These TPH subtypes can stay (still useful as a role marker), but the FK is now redundant — the venue/artist link comes from membership.
8. Re-scaffold User + Venue + Artist + Organization migrations.
9. Update integration tests across all affected modules. Add tests for the multi-member case (one venue, two users with different roles).

**Risk:** auth refactor is mechanical but wide. Sweep with grep, not by intuition — `grep -r "user.GetId\|UserId != currentUser\|\\[Authorize(Roles ="` and triage each hit.

**Why it's in scope:** without this, you have a half-built model — Organization owns the compliance/Stripe, but the user-to-venue link is still the old 1:1 FK. Future "invite a teammate" feature would require revisiting every auth touch-point. Cheaper to do once.

## 6. Migration sequence reminder

After Phase 2 onwards, the canonical `./initial-migrations.ps1` order is:

```
1. SharedDbContext
2. UserDbContext
3. OrganizationDbContext    ← new
4. ArtistDbContext
5. VenueDbContext
6. ConcertDbContext
7. ContractDbContext
8. PaymentDbContext
9. CustomerDbContext
10. MessagingDbContext
11. PersistedGrantDbContext (Auth)
```

Cross-module FKs from Venue / Artist / Payment → Organization are **CLR-only navs are forbidden** (per `feedback_cross_context_fk.md` and `feedback_module_boundaries.md`). They are plain `int OrganizationId` properties. The database enforces FK constraint only because Organization migrates first; no navigation property is added.

## 7. Test impact

| Project | Impact |
|---|---|
| `Concertable.Organization.UnitTests` | NEW — `ComplianceContext` validation, `SellerIdentifier` round-trip, factory rules |
| `Concertable.Organization.IntegrationTests` | NEW — controller + service end-to-end, Stripe account provisioning chain |
| `Concertable.Venue.IntegrationTests` | Venue creation now also creates Organization. Rollback test for Org-fail-Venue-rollback |
| `Concertable.Artist.IntegrationTests` | Same as Venue |
| `Concertable.Payment.UnitTests` + `IntegrationTests` | `PayoutAccount` re-keyed; mocks updated |
| `Concertable.Concert.IntegrationTests` | Snapshot assertions at Accept; settlement reads snapshot |
| `Concertable.IntegrationTests.Common` | `MockStripeAccountClient` accepts OrganizationId |

## 8. Risks + mitigations

| Risk | Mitigation |
|---|---|
| EF Core nested owned types behave unexpectedly | Phase 1 has a full persistence round-trip test before any other phase depends on it |
| Stripe re-key in Phase 3 breaks existing dev environments | Dev seeder updates land in same phase; `./initial-migrations.ps1` regenerates clean |
| Snapshot misses a field needed by settlement | Phase 4 has explicit code-review checklist; assertion tests for "settlement called X, used snapshot Y not current state" |
| Organization creation race during Venue/Artist creation | Phase 2 wraps both in `IUnitOfWorkBehavior` over `OrganizationDbContext` + `VenueDbContext` (cross-context outbox if needed — defer to actual implementation review) |
| Compliance not captured at signup → cold-start settlements fail | Phase 5 lands the UI; Phase 4 should soft-fail (log + skip) until UI is shipped, then flip to hard-fail |

## 9. Estimated effort

| Phase | Effort estimate (working days) |
|---|---|
| Phase 0 — scaffolding | 1-2 |
| Phase 1 — compliance value object | 2-3 |
| Phase 2 — Venue/Artist FK to Organization | 2-3 |
| Phase 3 — Stripe re-key | 2-3 |
| Phase 4 — Booking snapshot | 2-3 |
| Phase 5 — Setup UI | 3-5 |
| Phase 6 — Multi-user membership + auth sweep | 3-5 |
| **Total** | **15-24 working days** |

These are calendar-realistic, not optimistic. Pad for migration retries, EF surprises, and dashboard-overlap coordination.

## 10. After this refactor

Things that **become easier** post-refactor and should be planned separately:

- **DAC7 annual XML export** — iterates `OrganizationEntity` rows, reads `Compliance`, queries Stripe for verified bank details, emits HMRC schema. Single export job, runs once a year. ~2-3 days work the first time it's needed.
- **"Invite teammate" UI** — model already supports it (Phase 6 lands the membership table). UI work only: invite-by-email flow, role picker, member list page. ~3-5 days.
- **Org-switcher UI** — for a user who actively manages multiple orgs. UI/session-state work; backend already supports it. ~2-3 days.
- **Same legal entity acting as both Venue and Artist** — DB constraint already permissive (Phase 2 uses non-unique FK). Just need UX to expose "add an artist profile to this venue org" and switch context in the SPA. ~3-5 days when it's a real customer ask.
- **Versioned platform settings** (VAT rate changes, fee schedule changes) — replace `PlatformFeeSnapshot` source with a `PlatformSettings` table that has `EffectiveFrom`/`EffectiveTo` rows. ~2 days.
- **More granular membership roles** (Finance, Staff, Door, Sound) — extend the `MembershipRole` enum + auth policy table. Roughly 1 day per role + the UI for assigning it.

None of these are blocked by the current refactor. They become weekend-sized tickets once the Organization + Membership model is the load-bearing concept.

---

## Decision log

- **2026-05-18** — Path 2 chosen over Path 1 (interface-only scaffolding). Rationale: compliance fields belong to the legal entity from day one; deferring the entity makes Phase 4 (snapshots) cleaner.
- **2026-05-18** — Membership table **brought back into scope** (Phase 6) after user pushback on "build it complete." Rationale: avoids a second sweep across all auth touch-points later; pre-launch cost is bounded and well-defined.
- **2026-05-18** — Spelling: `Organization` (US) to match codebase convention.
- **2026-05-18** — Venue/Artist FK to Organization is **non-unique** (DB level). Service-layer enforces "one Org has at most one Venue and one Artist." Reason: model is forward-compatible for "one Org acts as both Venue and Artist" without re-migration.
- **2026-05-18** — IDP unchanged. Role claim remains the authoritative gate; OrganizationId is backend-resolved from currentUser → memberships.
