# Concertable.B2B — Technical Debt

When an item is fixed, update both this file and [`ARCHITECTURE.md`](./ARCHITECTURE.md).

---

## MEDIUM

### Imported guidance still contains monorepo-era paths and runtime rosters

The lifecycle/product pointers now distinguish current B2B stages from configurable-workflow targets,
but the broader imported guidance has not had a source-accuracy pass in this microrepo. Examples include
the `.NET 9` label and old event/processor rosters in `ARCHITECTURE.md`, `api/TECH_DEBT.md` in
`CODE_PATTERNS.md`, and the `api/...` paths and historical symbol examples in Deal's architecture.
Existing debt entries also link outside this repository to former monorepo plans. Treat those paths
and symbol lists as unverified, not instructions to recreate removed types. Repairing the whole imported
corpus is separate from the configurable-workflow context update.

The microrepo has no `.agents/skill-routes.json` or frozen-tree `.agents/hooks/docs_reachability.py`;
the installed workflow helper also assumes repo-local skill bodies for its rules manifest. Manual
changed-link checks do not establish whole-corpus reachability. Keep generic enforcement in its standards
owner rather than copying a second handwritten implementation into B2B.

**Resolves when:** imported guidance and debt references match this repo's current source and durable
owners, current rosters are clearly separated from historical examples/targets, and the standards-owned
guidance/skill tooling validates this installed-plugin microrepo layout and its instruction reachability.

### Operation-claim idempotency is copy-pasted per entity, in three different shapes

Five long-running operations anchor themselves to a row with an operation id, and no two do it the same way.
There is no shared domain vocabulary for "this row is claimed by this operation", so each entity invents one.

| Entity | Field | Shape |
|---|---|---|
| `ApplicationEntity` | `AcceptanceOperationId` | caller supplies the id; `??=` then throw if it differs |
| `ConcertEntity` | `SettlementOperationId` | the entity mints the id (`??= Guid.NewGuid()`); a separate `Ensure` method throws two different ways |
| `ConcertEntity` | `CancellationOperationId` | — |
| `BookingEntity` | `OperationId`, `CancellationOperationId` | neither a `Begin` nor an `Ensure` — a third shape |

The two decisions that actually vary — who mints the id (caller or entity), and claim-versus-verify — are
answered differently each time, so an operation that spans entities cannot reason about a claim uniformly.
`ApplicationEntity.BeginAcceptance` also assigns before validating the assignment, and carries a no-argument
overload whose only caller is a unit test.

**Resolves when:** a composed domain type owns the claim, following the `EventRaiser` precedent — a small
sealed class the entity holds rather than a base class it inherits — with one instance per claimable
operation and a single vocabulary (`Claim` / `IsHeldBy`) that every entity above uses. Unlike `EventRaiser`
this one persists, so the design must settle how the backing value maps (owned type or mapped backing field)
before the entities are migrated.

---

## HIGH

### The backend CI category filter silently skips untagged test projects

The backend workflow runs the solution with `--filter
"Category=Unit|Category=Integration|Category=Architecture|Category=Startup"`. Several projects contain
tests but no matching category traits, so `dotnet test` reports "No test matches" and exits successfully.
The affected non-E2E projects are Application unit, Artist unit, Booking unit, Conversations integration,
Conversations unit, User unit, and Venue unit. All seven pass when run directly — 117 tests — and CI never
runs any of them, so the workflow presents a green backend gate without executing those assemblies.

**Resolves when:** every intended backend test project either supplies a matching assembly/test category or
the workflow selects projects by an explicit tier manifest, and CI fails when any intended project discovers
zero selected tests.

---

### Authorization code is homed by accident-of-ownership, not by a real module

`PermissionAuthorizationHandler` / `PermissionRequirement` / `IMembershipContext` (request-time policy
checks) live inside `Concertable.B2B.Tenant.Infrastructure`/`.Contracts` purely because Tenant happens to
own the membership data they read — so *every* B2B module with a protected endpoint depends on
`Tenant.Contracts` for an orthogonal concern, the wrong dependency direction. `ManagerClients`
(registration-time role assignment, added by the auth-identity-model plan — a different lifecycle moment,
same underlying concern) now sits in `Concertable.B2B.Infrastructure/Authorization/` as a second instance
of the same accidental placement: the smallest correct move for *that* PR, not a resolution of the module
question.

**Not this item's fault, but this item's opportunity:** a genuine `Concertable.B2B.Authorization` module —
`.Contracts` (`PermissionRequirement`, a membership/role-fact facade interface Tenant *implements*);
`.Infrastructure` (`PermissionAuthorizationHandler`, `ManagerClients`); no `.Domain`/`.Api`, no persisted
aggregate of its own — would invert today's direction so every module depends on Authorization directly
instead of on Tenant. Materially bigger than any single feature PR: it rewires every B2B module's policy
registration. Tracked as `b2b-authorization/authorization-module` — see
[`plans/b2b-authorization/B2B_AUTHORIZATION_ROADMAP.md`](../../plans/b2b-authorization/B2B_AUTHORIZATION_ROADMAP.md).

**Resolves when:** `Concertable.B2B.Authorization` exists as above, `PermissionAuthorizationHandler` and
`ManagerClients` both live in it, no B2B module outside Authorization/Tenant itself references
`Tenant.Contracts` for a permission or role-fact check, and every module's policy registration goes through
Authorization.

---

### Workers uses `AddInMemoryTransport`, not ASB

`Concertable.B2B.Workers/ServiceCollectionExtensions.cs` line 35 wires `services.AddInMemoryTransport()`. The Workers host cannot consume any cross-service events from the bus. Settlement triggers and payout reconciliation that belong in Workers run inside `Concertable.B2B.Web` today.

**Resolves when:** `ServiceCollectionExtensions.cs` calls `services.AddAzureServiceBusTransport(...)` with `ServiceName = "concertable-b2b"` and subscribes the relevant events (`PaymentSucceededEvent`, etc.) to the Workers handlers.

---

### Venue dashboard revenue reads a table nothing writes

`VenueDashboardService` (`GetAsync`, `GetPaymentRevenueAsync`) calls `IPaymentReportingClient.GetPaymentRevenueAsync` /
`GetPaymentRevenueByMonthAsync`, which sum `PaymentTransactionEntity` rows. The only writer of that entity is Payment's
`PaymentTransactionRecorder`, registered under the keyed value `TransactionTypes.Payment` (`"payment"`). Nothing in the
system ever emits that key: `PaymentSessionProviderRequest` stamps the metadata `type` with the operation's own
`OperationType`, and the only payment-kind operation is Customer's ticket purchase, whose type is `"ticket-purchase"`.
The venue revenue KPI and its six-month chart are therefore structurally zero, with no exception and no failing test —
`MockSettlementClient` hard-codes `Money.Gbp(0m)` and `[]`, so the B2B suite cannot see it.

This is not introduced by the v1 cut-over's renaming: the pre-v1 `GetTicketRevenueAsync` summed the same table against
the then-live `TransactionTypes.Ticket` key, which v1 deleted. B2B had no other reporting query to migrate to.

**Resolves when:** the venue revenue widgets read the `ConcertSalesProjection` below instead of Payment, or Payment keys
its transaction recorder on the operation kind rather than a `type` string no producer emits (and adds `AmountMinor` to
`PaymentSessionProviderRequest.MetadataOf`, which the recorder reads and nothing writes).

---

### No `ConcertSalesProjection`

There is no sold-count / gross-revenue projection. B2B dashboards and settlement math can't read authoritative ticket sales data from Customer.

**Depends on:** Customer publishing `TicketPurchasedEvent` (see `api/Concertable.Customer/TECH_DEBT.md`).

**Resolves when:** `TicketPurchasedEvent` exists in Customer; B2B.Workers subscribes and writes a `ConcertSalesProjection` entity (concertId, soldCount, grossRevenue) into B2B DB, owned and read by the Concert module via its own context.

---

### E2E boots the whole real system from source references (won't survive the repo split)

`Concertable.B2B.E2ETests/AppFixture.cs` launches `Concertable.B2B.AppHost` via
`DistributedApplicationTestingBuilder.CreateAsync<Projects.Concertable_B2B_AppHost>()`, which composes
**real** Payment + Auth + Search through `Projects.Concertable_*` *source* references. That's fine in
the monorepo, but it's full-system E2E run from inside one service's repo — it conflates two test tiers
and breaks at the repo split (the `Projects.Concertable_Payment_*` types vanish once Payment is a
separate repo). E2E must never stub Payment (stubbing defeats E2E), so the fix is not "fake it here" —
it's to split the tiers by *where they run*:

**Resolves when:**
- **Per-repo (every PR):** B2B keeps only its **integration** tests, with the adapter services faked
  behind their contracts — Payment via the existing `MockManagerPaymentClient` / `MockEscrowClient` /
  `MockCustomerPaymentClient` against `Payment.Contracts` — plus **consumer-driven contract tests** so
  the fakes can't silently drift. No Payment source or runtime needed.
- **Full-system E2E (rare / pre-release, centralised — not per-service-repo):** stands up the
  real system from **published container images** (`AddProject<Projects.Concertable_Payment_Web>()` →
  `AddContainer("payment", "<registry>/payment:<version>")`). Same real Payment, pulled not compiled.
  This suite moves out of B2B's repo into a system/deployment pipeline.

Tracked by [`plans/platform/REPOSITORY_PER_MICROSERVICE_MIGRATION_PLAN.md`](../../plans/platform/REPOSITORY_PER_MICROSERVICE_MIGRATION_PLAN.md), stages 3 and 4.

---

## MED

### `Concertable.B2B.E2ETests/AppFixture.cs` hand-duplicates a subset of the real host's DI registrations, and can silently under-provision

The seed-only `Host` built in `AppFixture.cs` (`InitializeAsync`, ~line 157) re-lists its own subset of
`B2BWebHostExtensions.AddB2BWebHost`'s registrations rather than reusing it, because it only needs enough
to run `IDevSeeder`s. This drifted out of sync once: `ConcertDevSeeder` depends on `ITenantModule`, whose
graph (via `IVerificationService`) now reaches `IVenueModule`/`IArtistModule` → `VenueService`/
`ArtistService` → `IImageService` — a real, load-bearing dependency this seed host never registered
(`services.AddSharedImaging()` was missing; added alongside this note). Nothing catches this class of gap
at compile time; it only surfaces as an E2E `IDbInitializer` resolution failure the next time a module's
cross-module facade graph grows to reach a shared service this host omits.

**Resolves when:** the seed host's `ConfigureServices` is built from the same registration list as
`AddB2BWebHost` (e.g. factoring the shared-service registrations `AddB2BWebHost` and this fixture both
need into one call), so a facade graph reaching a new shared dependency can't silently leave one host
under-provisioned relative to the other.

---

### Venue opportunity counts are exposed by the write repository

`IOpportunityRepository.GetOpenWithApplicationCountsByVenueTenantIdAsync` is a read-only dashboard
projection, but it currently lives on the write repository and queries `ConcertDbContext` with
`AsNoTracking`. That is behaviourally safe, but it blurs the repository permission boundary and makes
`OpportunityDashboardService` depend on the write surface for a query. The projection belongs on
`IOpportunityReadRepository`, implemented by `OpportunityReadRepository` against
`IConcertReadDbContext`; the read context already provides the correct no-tracking stance, so this
should not be solved by restoring a general-purpose `Query` escape hatch.

**Resolves when:** the projection and its tests move to `IOpportunityReadRepository` /
`OpportunityReadRepository`, `OpportunityDashboardService` reads it through that interface, and
`IOpportunityRepository` no longer exposes the query.

---

### `DELETE api/organization` is a local hard-delete with no cross-module / cross-service teardown

`TenantService.DeleteAsync` deletes the tenant row and cascades only the Tenant module's own children (memberships, invitations). It emits **no `TenantDeletedEvent`** and touches nothing outside the `tenant` schema, so deleting an organization silently **orphans** everything provisioned off it: the Payment Stripe payout account (provisioned by `CredentialRegisteredHandler`), the venues/artists/concerts owned by the tenant (separate modules/contexts, no cross-schema FK — so no error, just dangling rows), and downstream Search projections. The create path deliberately re-raises `TenantCreatedDomainEvent` via `Announce()`, which `TenantCreatedDomainEventHandler` turns into the integration `PayoutOwnerRegisteredEvent`, for exactly this cross-service reason; delete has no symmetric path. Landed as a simple synchronous endpoint in the member-management phase (Phase 6.2); the full teardown is its own design (a new integration event + a Payment consumer that deactivates the connected account + module-owned cleanup of venue/artist/concert data).

**Resolves when:** tenant deletion publishes a `TenantDeletedEvent` (registered `Publishes<>`), Payment deactivates/closes the connected Stripe account on it, the Venue/Artist/Concert modules clean up (or soft-delete) their tenant-owned rows via their own handlers, and Search drops the corresponding projections — no owned data outlives the tenant.

---

### Venue and Artist duplicate a four-method tenant-keyed surface with no shared seam

`IVenueRepository` and `IArtistRepository` each declare the same four tenant-keyed reads —
`GetByTenantIdAsync`, `GetDetailsByTenantIdAsync`, `ExistsByTenantIdAsync` and `GetContactByTenantIdAsync` —
differing only in the module-owned return type (`VenueEntity`/`ArtistEntity`, `VenueDetails`/`ArtistDetails`),
and `VenueService`/`ArtistService` consume them in matching shapes. Today the duplication sits *inside* each
module rather than as a branch in a key-agnostic consumer, which is why it reads as ordinary module
separation rather than a smell.

Two things make it debt rather than acceptable symmetry. A third `TenantType` would need a third verbatim
copy of the whole surface, so the shape has a scaling cliff nobody has priced. And there is no declared seam
for a cross-module consumer, so the next component needing "the venue-or-artist thing for this tenant" will
reach for dual-injection and a `TenantType` branch — which is exactly the debt just resolved in
`VerificationService`, re-created one module over.

What exists now is the `ITenantStrategy` / `ITenantStrategyFactory<TStrategy>` spine in the Tenant module
over the shared `KeyedStrategyBuilder<TKey>` (`api/src/Concertable.B2B.KeyedStrategies`), with
`ITenantContactResolver` as its only member. Only `GetContactByTenantIdAsync` is promoted to the module
facades, and after that change nothing in production injects both `IVenueModule` and `IArtistModule`. The
other three reads are deliberately **not** promoted — a facade adapts a use case rather than mirroring a
repository, and no cross-module consumer wants them yet.

**Resolves when:** each new cross-module tenant-keyed need joins the `ITenantStrategy` family as its own
member instead of dual-injecting both facades, and the parallel four-method surface has a declared shape so
a new `TenantType` does not mean a third verbatim copy.

No second member is queued today. The tenant-deletion teardown above is **not** one: it is pub/sub fan-out —
each module handles the integration event for its own rows, mirroring how creation fans out — so it selects
nothing by key. The realistic next member is a cross-module *read* that varies by tenant type, such as a
Tenant-side "has this tenant provisioned its profile yet?" over `ExistsByTenantIdAsync`.

### Container image digests are hand-maintained in three places that nothing reconciles

`local/AppHost/AppHost.cs` pins auth, auth-migrations, payment-web, payment-workers and
payment-migrations; `tests/E2ETests/Concertable.B2B.E2ETests/AppFixture.cs` pins the search trio and
overrides payment-web/payment-workers with their E2E digests; and `.github/workflows/e2e.yml` pre-pulls
its own literal copy of that whole set to fail fast on registry access. Nothing compares the three.

The `origin/main` reconciliation is what exposed it. That branch bumped every auth, payment and search
digest and added two migration images, but never touched `e2e.yml` — so the workflow was left pulling a
stale auth image and two payment digests the fixture no longer runs, and the merge would have inherited
that drift silently. This is the same failure shape as the release-candidate set before `85c7c5a9`: a set
written out more than once, where missing a copy stays invisible until CI fails for an unrelated-looking
reason.

The workflow's copy is the one with no compiler behind it. The durable fix is to derive the pull list
from the two C# sources rather than restate it — the digests are already `const string` fields, so a
small script can emit the list the workflow consumes.

**Resolves when:** `e2e.yml` no longer contains literal image digests, and the set it pulls is generated
from, or verified against, the `AppHost`/`AppFixture` constants by a step that fails on divergence.

---

## RESOLVED

### ✅ Seed `TicketsSold` depends on the Payment seed simulator

Decided in favour of **reflection-set** (`plans/PAYMENT_SEED_REFLECTION_REFACTOR.md`). `ConcertFactory`
now sets `ConcertEntity.TicketsSold` via `.With(nameof(ConcertEntity.TicketsSold), spec.TicketsSold)`
from a `ticketsSold` field on `ConcertSeedSpec`, so seed concerts carry a deterministic sold count with
no event round-trip and no dependency on a Payment seed simulator (which no longer exists). The
divergence-from-production concern is accepted here because past-dated ticket sales are **inherently
unreproducible** — real Payment only emits `PaymentSucceededEvent` for live Stripe webhooks, and you
can't buy a ticket to a concert that already happened. Documented as a sanctioned exception in
the `seeding` skill. The settlement E2E (`ConcertFinishedTests`) reads these via
`TicketsSold * Price`: Past DoorSplit (id 12) and Past Versus (id 9) are seeded `ticketsSold: 1` —
the Versus concert was a real gap the old simulator catalog (concerts 13/12/10) omitted.

---

## LOW

### Committed Reqnroll `.feature.cs` files carry trailing blank lines the generator no longer emits

The eleven generated files under `tests/E2ETests/Concertable.B2B.E2ETests.Ui/Features/` each end with 30
trailing blank lines past what the current Reqnroll generator produces, so any build that regenerates them
dirties the working tree with a whitespace-only diff in all eleven. Nothing else differs.

**Resolves when:** either the regenerated output is committed, or the generated files stop being tracked.

### Action-link hrefs are hand-interpolated instead of generated from routes

Every `ActionLink` in the Api layer builds its href with string interpolation — `ApplicationMapper`,
`ConcertMappers`, `SelfBillingAgreementMappers` and `OpportunityMapper` (all in
`Concert.Api/Mappers/`) plus `Conversations.Api/Mappers/MessageMappers` between them interpolate
roughly a dozen `$"/api/..."` literals. Nothing ties a literal to the controller route it names, so a
route rename leaves the emitted link pointing at a 404 and no build or test fails. The frontend
compensates by stripping the `/api` prefix back off with a regex before re-issuing the call —
`apiPath` in `app/web/b2b/shared/src/features/concerts/api/actionLinkApi.ts` — which only works while
every literal happens to agree.

`Href` (Concertable.Kernel) now validates the string at construction, but validating a string someone
already hand-built is a checkpoint after the fact, not the fix — `LinkGenerator` means nobody builds it.

**Resolves when:** the Api-layer mappers take `LinkGenerator` (or `IUrlHelper`) and produce every
`ActionLink` from a named route rather than an interpolated literal, and the frontend's `apiPath` regex
is deleted because the emitted href is already client-relative.

---

### Admin verification queue enriches contact per row, not per page

`VerificationService.GetPendingAsync` (Tenant.Infrastructure) awaits `IVenueModule`/`IArtistModule`
`GetContactByTenantIdAsync` once per pending row, sequentially (required — see the fixed concurrency bug
this replaced: parallel calls hit the same scoped Venue/ArtistReadDbContext instance). Correct, but O(N)
queries per page instead of one batched lookup per `TenantType` on the page.

**Resolves when:** `IVenueModule`/`IArtistModule` gain a `GetContactsByTenantIdsAsync(IEnumerable<Guid>)`
batch method, and the admin service groups pending rows by `TenantType` and issues one call per group
instead of per row.

---

### Application affordances are not yet modelled as role-and-state discriminated unions

Application responses need different affordances for venue and artist callers, and those affordances also vary by
application lifecycle state. The current non-preview design uses `ApplicationResponse<TActions>` with separate venue
and artist action objects; nullable links within each role-specific object intentionally mean that an action is not
available in the current state. This keeps the two actor cases separate, but the type system still permits invalid
combinations such as checkout and withdraw being populated together.

**Resolves when:** after the repository upgrades to a .NET/C# version with production-ready discriminated unions and
stable `System.Text.Json` / OpenAPI support for them, replace the role-specific nullable action objects with exhaustive
role-and-state unions. Each variant must carry only its valid links, and the API mapper plus TypeScript contracts must
handle every variant exhaustively so invalid affordance combinations are unrepresentable end to end.

---

### Contract PDFs share the `images` blob container and rely on app-level write-once

`ContractPdfService` stores contract PDFs under a `contracts/{bookingId}-{guid}.pdf` name in the **single shared `"images"` container** (the only container `Concertable.Shared.Blob` exposes). The blob *name* is fixed at creation, transactionally, at Accept (`ContractEntity.Create`), so generation can't race to mint competing names — but immutability of the *bytes* is still only app-level: `IBlobStorageService.UploadAsync` is `overwrite: true`, so nothing at the storage layer prevents a rewrite of a persisted legal document. A legal artefact ideally lives in its own container with a no-overwrite (write-once / immutability-policy) upload. Deliberately not done in the contract feature because both are **additive changes to the published `Concertable.Shared.Blob` package** (a dedicated container config + an overwrite-guarding `UploadAsync` overload), which would cross the package boundary the feature was scoped to avoid.

**Resolves when:** `Concertable.Shared.Blob` gains a dedicated-container + write-once upload path, contract PDFs move to it, and `AttachPdf`'s app-level guard is backed by a storage-level immutability guarantee.

---

### `ContractEntity` "created only at Accept" is convention, not an enforced invariant

`ContractEntity`'s terms are immutable once built (private setters + `Create` factory), but nothing binds `Create` to the Accept transition — that timing lives in `ContractIssuer`/`AcceptExecutor`, so a future caller could mint a contract outside Accept and the model wouldn't stop them. `VenueTenantId`/`ArtistTenantId` are also publicly settable (for the tenant interceptor + issuer), so the snapshot isn't fully sealed either. Not addressed in the DEAL_RENAME refactor, which was names-only.

**Resolves when:** the Accept aggregate owns contract creation (e.g. `Create` becomes internal to the transition, or the booking aggregate is the only path that can produce one), and the tenant fields are stamped through a constructor/interceptor seam rather than public setters.

---

### `deal.Fee`/`HireFee` are `decimal` domain fields lifted to `Money` at the payment boundary

The money value-type migration (PR1 #390 → sync #393) made every
payment-client + `ISettlementAmountResolver` signature `Money`-typed, but `FlatFeeDeal.Fee` /
`VenueHireDeal.HireFee` (contracts + `*DealEntity`) stayed `decimal`. Checkout and the confirm strategies
lift them with `Money.Gbp(deal.Fee)` at the call sites — a legitimate
boundary conversion (same pattern as Customer's `Money.Gbp(concert.Price * qty)`), but it assumes GBP and keeps
a money value untyped in the domain, inconsistent with `EscrowEntity.Amount` which is a `Money` EF
ComplexProperty. Deferred from the sync PR because the field-type change needs an EF ComplexProperty mapping +
a DB re-scaffold that couldn't be verified in the disk/MAX_PATH-constrained environment at the time.

**Resolves when:** `Fee`/`HireFee` become `Money` (contracts + entities), mapped as a ComplexProperty like
`EscrowEntity.Amount`, the deal mappers + read sites cascade, migrations are re-scaffolded, and the
`Money.Gbp(deal.Fee)` boundary lifts collapse to plain `deal.Fee`.

---

### VAT / seller-id validation is format-only (regex), not verified against an authority

`UkDac7Strategy.IsValidVatNumber` checks only the *shape* of a VAT number (a regex from `UkDac7Options.VatNumberPattern`) — it proves the value looks like a UK VAT number, not that it's a real, active registration. DAC7's obligation is to *collect and verify* seller tax identity; format-only is the weak end of "verify". Stronger options, all pluggable behind the existing per-region `IDac7Strategy` seam without touching the gate / nag / form: (1) an offline **checksum** — UK VAT numbers carry a mod-97 check digit — to catch typos a regex passes; (2) **live verification** — HMRC's "Check a UK VAT number" API (returns a consultation reference number, itself useful audit evidence for the 2028 export) or, for EU sellers, VIES. Before building our own, check what **Stripe Connect** already collects/verifies on connected accounts — we may be about to re-solve tax-ID verification Stripe already does.

Deliberately not done now: the launch gate is *data completeness* (hold a complete, jurisdiction-valid tax identity for everyone we pay), not live verification. Live checks are async/networked (need caching + graceful degradation) and overlap Stripe — scope this onboarding blocker doesn't take on. Naturally lands with the DAC7 verification/export hardening (first export Jan 2028).

**Resolves when:** VAT (and other seller-id) validity is checked beyond format per jurisdiction — minimally an offline checksum, ideally a live authority check (HMRC / VIES) or a confirmed reuse of Stripe's tax-ID verification — implemented as the per-region `IDac7Strategy` behaviour, with the stored value staying a lenient `string?`.

---

### B2B portal frontend URLs have no non-local config — prod invite links would break

`FrontendUriGenerator` (`Concertable.B2B.Infrastructure`) resolves the venue/artist portal base per tenant type from `Urls:Frontends:{Venue,Artist}`. Those keys exist only as **localhost** in `Concertable.B2B.Web/appsettings.json`; there is no per-environment (App Config / tfvars) source for the real `venue.`/`artist.concertable.co.uk` hosts — that whole cloud-config layer is still the blocked future work in [`../../plans/platform/DOMAINS_AND_DNS.md`](../../plans/platform/DOMAINS_AND_DNS.md). So in any non-local environment the tenant-type dictionary binds empty and an invite send throws `KeyNotFoundException` — fails loud (not a silent bad link), but still broken.

**Resolves when:** `Urls:Frontends:{Venue,Artist}` are supplied per environment from App Config, alongside `Auth:SpaClients` / `Cors:AllowedOrigins` (which key off the same hostnames), as part of the `DOMAINS_AND_DNS.md` config rollout.

---

### The `[Admin]` authorization seam is thin, and there is no admin UI for moderation

`AdminAttribute` (`Admin.Api/Authorization`) resolves an `AdminProfileEntity` — a bare `Sub` column with
no roles and no scoping — through `AdminProfileHandler`, which issues an **uncached `AdminDbContext`
query on every request** to every `[Admin]` endpoint. Admin provisioning only happens via registration
through the `admin` client-id (`CredentialRegisteredHandler` calling `IAdminModule.GrantIfEligibleAsync`)
or `AdminTestSeeder`. Until the OSA
report-content work it was applied in exactly one place (`VenueController.Approve`); it now also gates
`ModerationController` (hide / restore / resolve / triage queue).

As an *authorization axis* this is correct and sufficient — it answers "is this caller a platform
operator?", which is precisely what those endpoints ask, and it is deliberately not tenant RBAC
(a `TenantRole` is scoped to one tenant and must never let a venue Owner moderate someone else's
conversation; an integration test asserts a tenant Owner gets 403 on every moderation endpoint). As an
*operations surface* it is not sufficient:

- **No admin SPA**, so moderation is Swagger/curl-driven at launch.
- **No admin roles**, so every operator has every admin capability.
- **A per-request uncached DB hit** on each `[Admin]` call.

The moderation feature compensates in its own data rather than by growing the seam: every action stamps
the acting user id and timestamp onto the report record, so the audit trail exists regardless. Accepted
at the expected near-zero report volume.

**Resolves when:** admin identity gains roles/scoping and a cached lookup, and an admin surface exists
to drive moderation — at which point the Swagger/curl workaround and this entry both go.

---

### Conversations has no retention policy

Messages accumulate forever. Nothing prunes them, and the Online Safety Act work deliberately hides
rather than deletes, so hidden content accumulates too.

The storage choice itself is not the debt — a relational store is correct for booking correspondence
that must be transactional with the booking flow and queryable for a regulator, and the specialised
stores chat products use would trade away exactly the properties this needs.

**Resolves when:** a retention policy is implemented. Its duration remains gated on the solicitor-owned
retention artifact in the OSA compliance pack, so it cannot be invented here.

---

### Content reporting is modelled as message-only and will not generalise as-is

`ContentReportEntity` lives in Conversations because a `MessageEntity` is the only reportable artifact
today, which is correct now and deliberately not abstracted early. But the Online Safety Act duty
attaches to **user-generated content**, and this platform has more of it: venue and artist profile text,
concert descriptions, uploaded images, and customer reviews. The Customer/marketplace OSA scope is
explicitly deferred with the marketplace, which is when those become in-scope.

The entity will not stretch to cover them. It carries a typed `MessageId` and a `ConversationId`, and is
reached through the conversation's access grants. A report against a venue profile belongs to no conversation, so
neither the foreign key nor the access shape fits.

**Resolves when:** a second reportable content type is actually required, at which point choose
deliberately between a polymorphic `(ContentType, ContentId)` report with per-type tenancy resolution,
or a per-module report entity behind a shared triage view. Do not pre-build either before the second
case exists.

### `app/web/shared` still hands B2B a Stripe payment-method id

`StripePaymentForm.onSuccess` is typed `(paymentMethodId: string) => void` and `NewCardSection.onConfirmed`
the same, so the shared web tier offers consumers the `pm_…` id it reads off the confirmed intent. B2B no
longer sends it — apply and accept post only the e-signature, and Payment resolves the method from the
reference — but the seam still exposes it, and a future consumer could pick it back up.

**Why it is not fixed here:** `carve-fe` builds each app against `@concertable/web` **as published to the
feed**, so narrowing the callback in `app/web/shared` and consuming the narrower shape in `app/web/b2b/*`
in one PR fails that gate. Same publish-first split as the `ApplicationActions.decline` entry.

**Resolves when:** `web` is republished with `onSuccess: () => void` / `onConfirmed: () => void`, the b2b
and customer callers drop the argument, and `carve-fe` is green.

---

### `Concertable.B2B.E2ETests` builds to `bin/` 14 characters from the native-path limit

`docs/LOCAL_DEV.md` records the measured 250-character cap on native DLL loading, which the four E2E host
executables now clear by building to `artifacts/e2e/` via `BaseOutputPath`. This project still builds to
`bin/`, where its `runtimes/win-x64/native/Microsoft.Data.SqlClient.SNI.dll` is 236 characters from a
101-character worktree root. A branch folder 15 characters longer than
`Refactor-launch_deal-lifecycle-modules-phase2` therefore kills the API E2E suite itself with
`DllNotFoundException ... (0x800700CE)` before a single test runs.

It cannot simply follow the four hosts, because two consumers address its output at its default location:
`scripts/local-platform.ps1`'s `Assert-DataAccessAssembly` scans `<project>/bin/<configuration>` for the
`Concertable.DataAccess.Infrastructure` version check, and `.github/workflows/test.yml` invokes
`playwright.ps1` at a literal `.../Concertable.B2B.E2ETests.Ui/bin/Release/net10.0/` path.

**Resolves when:** this project builds through a short artifacts root like the E2E hosts do, with both
consumers resolving the output path from MSBuild rather than assuming `bin/<configuration>/<tfm>`.

---

### `Concertable.B2B.TestKit.SeedState` names most of its seed handles `TestEntity`

`TestEntity(int Id)` stands in for a flat-fee application, a door-split application, a versus application,
a venue and two past applications — six different things behind one name that says nothing about any of
them. A reader of `fixture.SeedState.FlatFeeApp.Id` cannot tell from the type what else that handle could
carry, and any field a caller needs (an opportunity id, an artist id) has nowhere to live without either
widening the shared placeholder for every unrelated consumer or minting a one-off beside it — which is what
`TestApplication(int Id, int OpportunityId)` already is.

**Resolves when:** each seed handle has a record named for what it is (`TestApplication`, `TestVenue`, …)
carrying the fields that handle actually needs, and `TestEntity` is gone.

---

### Accept and apply checkout quote the base fee, not what the payer is charged

`ApplicationCheckoutService` returns `new FlatPayment(flatFee.Fee)` / `new FlatPayment(venueHire.HireFee)`
as the checkout amount, while Payment sizes the actual hold as fee plus the platform fee — £180 shown and
£190 taken for the seeded flat-fee deal, £300 shown and £310 taken for venue hire. The two flows now agree
with each other and with the escrow they create, so nothing reconciles wrongly; the number the payer reads
before confirming is simply not the number that leaves their card.

B2B cannot close this by arithmetic: the platform fee is Payment's, and duplicating it in B2B is the split
brain this branch removed. `IEscrowOperationsClient.AuthorizeAsync` already computes the payer total server
side and its `PaymentSessionDescriptor` is the natural place to return it.

**Resolves when:** the escrow authorization reports the payer total it charged, `Checkout` carries that
alongside the payee amount, and the B2B checkout surfaces the split rather than one figure that is neither.

---


### The image vulnerability gate tolerates one base-image package while it stays unfixed

`verify-artifact-integrity.ps1` fails CI on any HIGH or CRITICAL vulnerability in a candidate image.
`$toleratedUnfixedPackages` exempts one name from that — `linux-libc-dev`, from the Ubuntu 24.04 layer
of the Azure Functions base image the Workers host runs on — and only while a finding has no published
fix.

It is exempt because there is no version of that image where the gate passes. The package accounted
for 169 findings, 5 CRITICAL and 164 HIGH, and **every one of them is unfixed**: Ubuntu publishes no
corrected version, so there is nothing to upgrade to. They are kernel headers, and a container executes
the host kernel rather than anything in that package. Web and the seeding simulator sit on the ASP.NET
base image and report nothing, so this is the Functions host alone.

The exposure is not the kernel headers, which are inert here — it is that any suppression is a place
future findings can hide, so this one is drawn as tightly as it can be. Trivy scans at HIGH and
CRITICAL with `--ignorefile /dev/null`, every finding at those severities reaches the retained evidence
unfiltered, and the decision about what blocks is made afterwards in the script where it can be read.
A blanket `--ignore-unfixed` was rejected: it would also have silenced an unfixable CRITICAL in OpenSSL
or any other runtime dependency, which is exactly the case worth knowing about because it needs
mitigating another way. A package-scoped `.trivyignore.yaml` was tried first and does not work: Trivy
0.74 reads the file, but its `purls` matching does not match this package — verified against the real
image. The secret scan is unconditional at every severity.

**Resolves when:** Ubuntu ships a fixed `linux-libc-dev` in the Functions base image, or the Workers
host no longer runs on that base — at which point `$toleratedUnfixedPackages` becomes empty and the
helper goes with it. A *fixable* `linux-libc-dev` finding already fails today, so the gate reports the
day this stops being true rather than waiting to be noticed.

### `PublishContainer` pushes to ghcr.io by default, including from a developer's machine

`Directory.Build.props` sets `<ContainerRegistry Condition="'$(ContainerRegistry)' == ''">ghcr.io</ContainerRegistry>`,
so `dotnet publish -t:PublishContainer` on any of the three container projects **pushes to the
production registry** unless the caller overrides it. There is no confirmation step and no tag
namespacing that would make an accidental push obviously a test.

CI is not exposed: every image step passes `ContainerArchiveOutputPath`, which redirects the output to
a tarball and never contacts a registry. The exposure is a developer or an agent running the documented
publish command directly. It was found exactly that way — the command was run to inspect image
metadata, and the only thing that stopped a real push to `ghcr.io/concertable/b2b-web` was the absence
of a credential in that shell. With `docker login ghcr.io` already done, which is ordinary for anyone
who has pulled a private image, it would have succeeded.

The property is deliberate — it is here rather than in a workflow so the carve carries it — so the fix
is not to delete it but to make the destination explicit at the call site: default to the local daemon
and have publication opt in, which is the direction that also lets the release-candidate flow scan and
push the *same* tar rather than rebuilding.

**Resolves when:** a plain `dotnet publish -t:PublishContainer` with no extra properties cannot reach a
remote registry, and the publish workflow names its registry explicitly.

---

### Publishable client keys were committed to `app/web/.env.development`

`.env.production` states the convention — "Publishable (client-side) keys — injected by CI at build,
blank in git on purpose" — and holds both keys blank. `.env.development` did not follow it: it carried
a live Google Maps API key and a Stripe **test** publishable key. Both are now blank there too, so
`HEAD` is clean and the release-candidate source scan passes.

**Blanking them does not unpublish them.** Both values are in this repository's history and in the
monorepo's, where the file has been tracked across fourteen commits going back to the earliest
frontend work. Promotion to public at `10C` makes that history readable, so the values must be treated
as disclosed regardless of what the current tree says.

The Stripe key is test-mode and publishable, which is the category designed to ship in client
JavaScript, so its disclosure carries no authority. The Google Maps key is the one that matters: an
unrestricted browser key is billable by anyone who finds it, and Trivy does not flag `AIza` prefixes at
all — the secret scan passed it silently and it was found by reading the file.

**Resolves when:** the Google Maps key is rotated and the replacement is restricted by HTTP referrer,
and the Stripe test key is rotated or consciously accepted as disclosed. Rotation is not something this
repository can do for itself; it belongs to whoever owns those consoles, and it should happen before
the repository becomes public rather than after.
