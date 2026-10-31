# Concertable.B2B — Architecture

> Cross-service design rationale and decision history: the `microservices-architecture` skill
> Internal module rules: the `dotnet-standards:module-structure` and `dotnet:module-structure` skills
> Outstanding gaps: [`TECH_DEBT.md`](./TECH_DEBT.md)

---

## Bounded context

B2B owns the venue/artist side of Concertable: opportunities, applications, bookings, contracts, concert workflow, settlement, and manager/admin profiles. Opportunity, Application, Booking and Concert are separate stage aggregates in separate modules; Concert is the final stage, not the entire recruitment/booking lifecycle. It consumes Booking's immutable `ConfirmedBooking` handoff, rather than loading live Application/Opportunity terms. Product direction and the target direct-invitation route are routed from [`AGENTS.md`](./AGENTS.md#product-direction--configurable-work-within-fixed-lifecycle-routes); the current lifecycle is owned by [`api/src/Modules/Concert/AGENTS.md`](./api/src/Modules/Concert/AGENTS.md). B2B does **not** own ticket buyers, customer reviews, or browse/search — those belong to Customer and Search respectively.

---

## Host topology

| Project | Kind | Purpose |
|---|---|---|
| `Concertable.B2B.Web` | ASP.NET Core HTTP host | All controllers + ASB event consumers (in-process). Composition root. |
| `Concertable.B2B.Workers` | Azure Functions isolated | Background jobs. Currently: `ConcertFinishedFunction` (hourly timer). Uses **in-memory transport** — see TECH_DEBT. |
| `Concertable.B2B.DataAccess` | Shared csproj | Shared data-access primitives: tenant query filters, the per-stance DbContext base classes, and repositories. |
| `Concertable.B2B.AppHost` | Aspire AppHost | Local-dev orchestrator only. |

**Database:** `B2BDb` (PostgreSQL). Artist, Venue, and Concert each use a tenant-bound tracked/write
`<Module>DbContext` plus a tenant-independent read-only `<Module>ReadDbContext`. Conversations also has
`ConversationsPrivilegedDbContext` for tenant-independent moderation writes
and `ConversationsReadDbContext` for the unfiltered subject-access read. Privacy coordinates read-only subject exports through module facades and owns no database schema.

---

## Modules

Modules live under `api/src/Modules/`. Each follows the `Concertable.B2B.<Module>.*` naming convention.

| Module | Canonical entities | Projects |
|---|---|---|
| **Artist** | `ArtistEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |
| **Opportunity** | `OpportunityEntity` (recruitment listing, state and `DealId`) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |
| **Application** | `ApplicationEntity` (application decision and accepted snapshot handoff) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **Booking** | `BookingEntity` (confirmation and pre-Concert cancellation), `ContractEntity` (accepted terms/signatures) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **Concert** | `ConcertEntity` (live concert, publication, cancellation and settlement; frozen booking snapshot), `InvoiceEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **Deal** | `DealEntity` (current TPT: `FlatFeeDealEntity`, `DoorSplitDealEntity`, `VersusDealEntity`, `VenueHireDealEntity`) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **Conversations** | `MessageEntity`, `ParticipantProfile` (local sender-display projection) | Api, Application, Contracts, Domain, Infrastructure |
| **Tenant** | `TenantEntity` (business account, membership boundary, legal/VAT/Stripe identity, settlement payee) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **User** | `UserEntity` (flat) + standalone `AdminProfileEntity` — no TPH, no manager-profile subtypes | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |
| **Venue** | `VenueEntity`, `VenueImageEntity`, `PayoutAccountEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |

Cross-module calls go through `IXModule` facades in `Concertable.B2B.<Module>.Contracts` only. No direct entity reach-in between modules.

`Tenant` is the canonical backend concept. In the current one-to-one model it is both the isolation boundary and the business/legal account; there is no separate organisation aggregate or identifier. Artist and Venue are tenant-owned marketplace profiles. Organisation wording belongs only at presentation boundaries.

---

## Integration events

All event types implement `IIntegrationEvent` from `Concertable.Messaging.Contracts`. Transport: Azure Service Bus (`concertable-b2b` service name, `event-` topic prefix). Wired in `Concertable.B2B.Web/Program.cs`.

### Published

| Event | Defined in | When raised |
|---|---|---|
| `ArtistChangedEvent` | `Concertable.B2B.Artist.Contracts.Events` | Artist profile updated |
| `ArtistRatingUpdatedEvent` | `Concertable.B2B.Artist.Contracts.Events` | Rating projection updated |
| `VenueChangedEvent` | `Concertable.B2B.Venue.Contracts.Events` | Venue profile updated |
| `VenueRatingUpdatedEvent` | `Concertable.B2B.Venue.Contracts.Events` | Rating projection updated |
| `ConcertChangedEvent` | `Concertable.B2B.Concert.Contracts.Events` | Concert edited / stage changed |
| `ConcertPostedEvent` | `Concertable.B2B.Concert.Contracts.Events` | Concert moves to Posted stage |
| `ConcertRatingUpdatedEvent` | `Concertable.B2B.Concert.Contracts.Events` | Rating projection updated |

The B2B host also dispatches published integration events to registered local handlers. Conversations consumes `ArtistChangedEvent` and `VenueChangedEvent` locally to maintain its `ParticipantProfile` projection keyed by `TenantId`; sender rendering never reaches into Artist or Venue storage synchronously.

### Consumed

| Event | Source | Handler(s) |
|---|---|---|
| `CredentialRegisteredEvent` | Auth | `CredentialRegisteredHandler` (User module — creates the role-agnostic user projection and the surviving admin profile) |
| `CustomerReviewSubmittedEvent` | Customer | `ArtistReviewProjectionHandler`, `VenueReviewProjectionHandler`, `ConcertReviewProjectionHandler` |
| `PaymentSucceededEvent` | Payment | `SettlementPaymentProcessor`, `EscrowPaymentProcessor`, `VerifyPaymentProcessor`, `TicketSaleProcessor` |
| `PaymentFailedEvent` | Payment | `BookingPaymentFailedProcessor`, `VerifyPaymentFailedProcessor` |

All consumed events use `InboxMessageEntity` deduplication keyed by `(MessageId, ConsumerName)`.

---

## Sync calls out

| Target | Client | Usage |
|---|---|---|
| `Concertable.Payment` | `Concertable.Payment.Client` | Create payment intents, transfers, refunds, payout account ops |
| `Concertable.Auth` | JWT Bearer middleware | Token validation; `client_credentials` for service-to-service tokens |

No sync calls to Customer or Search. B2B and Customer communicate **exclusively via bus events**.

---

## Authentication

- JWT Bearer, audience `concertable.b2b.api`
- `client_credentials` client: `concertable-b2b`, scope `payment:write`
- **No role claims in tokens.** B2B tokens are identity-only (`sub` + `email`); acting authority is the request-scoped active tenant membership (`X-Tenant-Id` → the member's roles), never a token claim. See [Authorization](#authorization).

---

## Authorization

B2B decides every permission in-process, from data it owns, in three layers: a generated **RBAC**
catalog says what each role may do and how widely; **ABAC** principal rules on each aggregate give the
venue and artist tenants access from the aggregate's own columns; **ACL** rows (member assignments and
concert shares) extend access to named members and other tenants. Permission changes take effect from
the member's next request; there is no authorization locking.

### Request authority

`TenantResolutionMiddleware` runs after authentication and asks `AuthorizationContext`
(`Authorization.Infrastructure`) to resolve the caller's membership in the active tenant: the
`X-Tenant-Id` header, or the caller's only membership when there is no header. The result is a
`MembershipSnapshot`, exposed for the rest of the request as `IAuthorizationContext.Membership`:

| Snapshot member | Meaning |
|---|---|
| `MembershipId`, `TenantId`, `UserId` | Who is acting, in which tenant |
| `PermissionVersion` | Bumped by the membership's role changes |
| `RolePolicyVersion` | The tenant's role-definition version, bumped when a role's permissions change |
| `Permissions` | `EffectivePermissions`: each held `TenantPermission` with its `ResourceAccess` |

`ResourceAccess` is `None`, `Assigned` or `All`. Tenant-wide routes (being a principal, receiving a tenant
share) need `All`; member routes (an assignment, a share to one membership) need `Assigned` or `All`.
The snapshot never changes during the request and compares by value.

### Generated catalog (RBAC)

Each module declares its permissions in `api/src/Modules/<Module>/authorization.json`: a
`resource:action` name, a label, whether it is owner-only, and the access each system preset holds.
`scripts/generate-authorization.py` writes the `TenantPermission` enum (`[JsonStringEnumMemberName]` keeps
the `resource:action` key on the wire), `AuthorizationCatalog` with a content-hash `Revision`, and the
frontend `TenantPermission` union. CI runs it with `--check`. The revision is seeded into the database
and checked by the health check and invitation acceptance.

```json
{ "name": "concerts:ops_edit", "label": "Edit concert operations", "ownerOnly": false,
  "systemPresets": { "Owner": "All", "Manager": "All", "Staff": "Assigned", "Sound": "Assigned" } }
```

`[HasPermission(TenantPermission.ConcertsOpsEdit)]` gates an endpoint when the snapshot holds the
permission at any access. It keeps callers without the permission out of the service; it does not decide
access to a particular resource.

### Principal rules (ABAC), assignments and shares (ACL)

Each aggregate decides what an actor may do to it, by what it decides:

| Aggregate | Principal access (venue and artist tenants, at `All`) | Assignments and shares |
|---|---|---|
| Application | read; venue decides (`IsDecidableBy`), artist withdraws (`IsWithdrawableBy`) | — |
| Booking | read; either principal cancels (`IsCancellableBy`) | — |
| Contract | read with `terms:read` | — |
| Concert | summary, operations, finance; venue edits and publishes, either principal cancels | `Assignments` (summary, operations); `Shares` (summary, optional `ExpiresAt`) |
| Invoice | read with `settlement:view` | — |
| Conversation | participants read and send | `Assignments` (read, send) |

`MemberAssignment(MembershipId, AssignedByUserId, AssignedAt)` is one shared value object, owned by
`ConcertEntity.Assignments` and `ConversationEntity.Assignments`; `ConcertShare` and
`ConversationParticipant` are owned by their aggregates. Unassigning and unsharing delete the row.

```csharp
var concert = await privilegedRepository.GetByIdForUpdateAsync(id, ct);
if (concert is null || !concert.IsEditableBy(expectedActor))
    return new UpdateConcertError.NotPermitted();
```

Reads go through the module's `ResourceScopedDbContext`, whose plain-lambda query filters apply the same
rules for the request's actor, so lists and single reads see exactly what the aggregate would allow. A
read gated by a different permission than the filter's (terms, settlement) narrows explicitly in its
repository.

```csharp
modelBuilder.Entity<BookingEntity>().HasQueryFilter(TenantFilters.Key, booking =>
    OperationsAccess == ResourceAccess.All
    && (booking.VenueTenantId == ActorTenantId || booking.ArtistTenantId == ActorTenantId));
```

Locks that protect domain invariants stay: tenant administration takes the tenant row `FOR UPDATE`
(last owner, role changes), and aggregates keep their own concurrency tokens and row locks.

### Request flow

```mermaid
sequenceDiagram
    participant Client
    participant Middleware as TenantResolutionMiddleware
    participant Context as AuthorizationContext
    participant Gate as PermissionAuthorizationHandler
    participant Service as Module service
    participant Db as PostgreSQL
    Client->>Middleware: request (+ X-Tenant-Id)
    Middleware->>Context: ResolveAsync()
    Context->>Db: read membership snapshot
    Context-->>Middleware: IAuthorizationContext.Membership
    Middleware->>Gate: [HasPermission] policy
    Gate-->>Client: 403 when the snapshot lacks the permission
    Gate->>Service: call
    alt read
        Service->>Db: query through ResourceScopedDbContext (principal, assignment and share filters)
    else write
        Service->>Db: load the aggregate
        Service->>Service: aggregate.IsXableBy(actor)
        Service->>Db: change, commit
    end
    Service-->>Client: result or the operation's typed error
```

---

## Internal architecture

B2B is a modular monolith *inside* the service. The `dotnet-standards:module-structure` and `dotnet:module-structure` skills apply verbatim:

- Cross-module calls: `IXModule` facade only (in `<Module>.Contracts`)
- Per-module `XDbContext` with its own schema; all point at `B2BDb`
- Intra-service events: in-process `IEventRaiser` + `XChangedDomainEvent` — no bus involvement
- Module-owned `IEntityTypeConfiguration<T>` in `<Module>.Infrastructure/Data/Configurations/`
- Per-module `IDevSeeder` / `ITestSeeder`
- `InternalsVisibleTo` chains; module Application interfaces stay `internal`

The bus (`AddAzureServiceBusTransport`) is for **cross-service** events only. Intra-B2B flows that currently round-trip ASB are flagged in TECH_DEBT.

---

## Tech stack

.NET 10 · EF Core + Npgsql/PostgreSQL · Azure Service Bus (ASB emulator in dev) · `Concertable.Messaging` (Outbox/Inbox/Transport) · NetTopologySuite (geometry) · Duende IdentityServer (Auth service) · Aspire (`Concertable.ServiceDefaults`) · SignalR (real-time notifications) · `Concertable.Shared.{Blob,Email,Geocoding,Imaging,Pdf}`

---

## What is NOT in this service

| Concern | Lives in |
|---|---|
| Ticket sales, ticket entities | `Concertable.Customer` |
| Customer reviews (`ReviewEntity`) | `Concertable.Customer` |
| Customer profile / preferences | `Concertable.Customer` |
| Browse / autocomplete / search reads | `Concertable.Search` |
| Stripe API keys, webhook receiver | `Concertable.Payment` |
| Identity authority (`sub`, password hash, email verification) | `Concertable.Auth` |
