# Concertable.B2B — Architecture

> Cross-service plan and design rationale: [`api/docs/MICROSERVICES_ARCHITECTURE.md`](../docs/MICROSERVICES_ARCHITECTURE.md)
> Internal module rules: [`api/docs/MODULAR_MONOLITH_RULES.md`](../docs/MODULAR_MONOLITH_RULES.md)
> Outstanding gaps: [`TECH_DEBT.md`](./TECH_DEBT.md)

---

## Bounded context

B2B owns the venue/artist side of Concertable: opportunities, applications, bookings, contracts, concert workflow, settlement, and manager/admin profiles. The concert here is a **workflow entity** (`Posted → Applied → Accepted → Verified → Finished → Settled`) with contract terms and settlement obligations. B2B does **not** own ticket buyers, customer reviews, or browse/search — those belong to Customer and Search respectively.

---

## Host topology

| Project | Kind | Purpose |
|---|---|---|
| `Concertable.B2B.Web` | ASP.NET Core HTTP host | All controllers + ASB event consumers (in-process). Composition root. |
| `Concertable.B2B.Workers` | Azure Functions isolated | Background jobs. Currently: `ConcertFinishedFunction` (hourly timer). Uses **in-memory transport** — see TECH_DEBT. |
| `Concertable.B2B.DataAccess` | Shared csproj | Shared data-access primitives: tenant query filters, the per-stance DbContext base classes, and repositories. |
| `Concertable.B2B.AppHost` | Aspire AppHost | Local-dev orchestrator only. |

**Database:** `B2BDb` (SQL Server). Per-module DbContexts for writes; each module's read-only `Public<Module>DbContext` (e.g. `PublicConcertDbContext`) for unfiltered / cross-tenant reads.

---

## Modules

All modules live under `Modules/`. Each follows the `Concertable.B2B.<Module>.*` naming convention.

| Module | Canonical entities | Projects |
|---|---|---|
| **Artist** | `ArtistEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |
| **Concert** | `ConcertEntity` (workflow: stage, BookingId, ContractType), `OpportunityEntity`, `ApplicationEntity`, `BookingEntity`, `SettlementTransactionEntity`, `TicketTransactionEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **Contract** | `ContractEntity` (TPH: `FlatFeeContractEntity`, `DoorSplitContractEntity`, `VersusContractEntity`, `VenueHireContractEntity`), `EscrowEntity` | Api, Application, Contracts, Domain, Infrastructure, UnitTests |
| **Conversations** | `MessageEntity` | Api, Application, Contracts, Domain, Infrastructure |
| **Notification** | SignalR hub (`NotificationHub` at `/hub/notifications`) | Contracts, Infrastructure — slim, no Domain/Application. Pending deletion after Phase 8 Step 24 (see TECH_DEBT). |
| **Tenant** | `TenantEntity` (org legal/VAT/Stripe identity; owns venues; settlement payee — the renamed `OrganizationEntity`) | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests, UnitTests |
| **User** | `UserEntity` + manager/admin profile subtypes (`VenueManagerEntity`, `ArtistManagerEntity`, `AdminEntity`). TPH unwind pending — see TECH_DEBT. | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |
| **Venue** | `VenueEntity`, `VenueImageEntity`, `PayoutAccountEntity` | Api, Application, Contracts, Domain, Infrastructure, IntegrationTests |

Cross-module calls go through `IXModule` facades in `Concertable.B2B.<Module>.Contracts` only. No direct entity reach-in between modules.

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

**Defined in Contracts but not yet published:** `ConcertSettledEvent`, `ConcertFinishedEvent`, `ConcertApplicationCreatedEvent`, `ConcertApplicationAcceptedEvent` — see TECH_DEBT.

### Consumed

| Event | Source | Handler(s) |
|---|---|---|
| `CredentialRegisteredEvent` | Auth | `CredentialRegisteredHandler` (User module — creates manager profile) |
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
- **No role claims in tokens.** Role derived per-controller from `ICurrentUser.Sub` ↔ User module manager/admin profile lookup.

---

## Internal architecture

B2B is a modular monolith *inside* the service. Rules in `api/docs/MODULAR_MONOLITH_RULES.md` apply verbatim:

- Cross-module calls: `IXModule` facade only (in `<Module>.Contracts`)
- Per-module `XDbContext` with its own schema; all point at `B2BDb`
- Intra-service events: in-process `IEventRaiser` + `XChangedDomainEvent` — no bus involvement
- Module-owned `IEntityTypeConfiguration<T>` in `<Module>.Infrastructure/Data/Configurations/`
- Per-module `IDevSeeder` / `ITestSeeder`
- `InternalsVisibleTo` chains; module Application interfaces stay `internal`

The bus (`AddAzureServiceBusTransport`) is for **cross-service** events only. Intra-B2B flows that currently round-trip ASB are flagged in TECH_DEBT.

---

## Tech stack

.NET 9 · EF Core + SQL Server · Azure Service Bus (ASB emulator in dev) · `Concertable.Messaging` (Outbox/Inbox/Transport) · NetTopologySuite (geometry) · Duende IdentityServer (Auth service) · Aspire (`Concertable.ServiceDefaults`) · SignalR (real-time notifications) · `Concertable.Shared.{Blob,Email,Geocoding,Imaging,Pdf}`

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
