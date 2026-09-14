# Deal layering and DI-mapper collapse plan

## Outcome

Two coupled corrections, in order:

1. **Layering** — a `*.Domain` project must not reference a `*.Contracts` project. Deal is the proving ground:
   its two shared enums move to the homes that actually own them, and `Deal.Domain` loses its
   `ProjectReference` to `Deal.Contracts`. The domain then cannot see a wire DTO, and the compiler enforces it.
2. **Mappers** — the remaining "mapper" families that carry no dependencies and no behaviour collapse into
   static, total, dependency-free mappers plus one honestly-named factory. 17 files become 4.

They are sequenced together because the layering fix decides where DTO→entity dispatch is allowed to live.
With the reference gone, that dispatch is unambiguously Application's, and the mapper reduces to the one
direction that is genuinely a mapping.

## Current baseline

Verified against `origin/main` @ `15ce7946f`.

### The layering defect

`Concertable.B2B.Deal.Domain.csproj:10` references `Concertable.B2B.Deal.Contracts`. Deal.Domain uses exactly
two things from it — `DealType` and `PaymentMethod`, both in the `Concertable.B2B.Deal.Contracts.Enums`
namespace. It uses no DTO. But the reference is assembly-wide, so `DealDto` (with its `[JsonPolymorphic]`
attributes), `DealTerms`, `IDealModule`, `CreateDealError`, `UpdateDealError` and the whole
`Contracts/Strategies` family sit on the domain's reference graph as collateral.

`Deal.Contracts` is therefore two projects wearing one name: shared **enums** (which Domain legitimately
needs) and **wire contracts plus strategy machinery** (which Domain must never see).

12 Domain projects across B2B, Messaging and Payment carry the same reference. The Customer service mostly does
not — `Customer.Ticket.Domain`, `Customer.Review.Domain` and `Customer.Venue.Domain` reference only
`Concertable.Kernel`. That is the target shape and it already exists in-repo.

`Deal.Contracts` is **not** a published package (B2B `Directory.Build.props:39` defaults `IsPackable` false and
Deal.Contracts does not opt in), so no enum duplication is required — there is no independent-versioning
argument for a domain `PaymentMethod` plus a `PaymentMethodDto`. One enum both sides can see is sufficient and
strictly simpler.

### The mapper defect

The remaining families named "mapper" carry no collaborators and exist only to re-dispatch on a key:

| Family | Files | Location | Per-arm body |
|---|---|---|---|
| `IDealMapper` | 6 | `Deal.Application/Mappers` | cast, project 2–4 properties |
| `IDealUpdater` | 6 | interface in `Deal.Application/Interfaces`, facade + 4 arms in `Deal.Infrastructure/Services/Updaters` | cast, call `entity.Update(primitives)` |
| `ITransactionMapper` | 3 arms + interface + facade | `Payment.Application/Mappers` | cast, project ~8 properties |
| `IUserMapper` | 2 | `B2B User.Infrastructure/Mappers` | no dependencies, fake-async `Task.FromResult` |

`ITransactionMapper` does not use DI at all — `TransactionMapper.cs:9` is a `FrozenDictionary` of hand-`new`'d
instances, so it pays the ceremony and receives none of the benefit. `IUserMapper` has no dependencies and
wraps a pure function in `Task.FromResult`.

Registration for the two Deal families is `Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs:47-66`
(scoped facades plus four keyed singletons each), with coverage enforced by `RequireAll` through
`Concertable.B2B.Infrastructure/Services/Strategies/DealStrategyBuilder.cs` over
`Concertable.B2B.KeyedStrategies/KeyedStrategyBuilder.cs`.

### The construct-and-discard validator

`DealService.Validate` (`DealService.cs:51`) answers "is this deal valid?" by calling `mapper.ToEntity(deal)`,
discarding the constructed entity, and keeping only the errors.

That is why `IDealMapper.ToEntity` returns `Result<DealEntity, ValidationErrors>` at all — construction (which
validates, via `FlatFeeDealEntity.Create`) was routed through something named "mapper", so the mapper inherited
a failure mode. A mapper is a total function; the moment it can fail it is not one.

### What main has already fixed

`IPaymentAmountMapper`, `IDealTerms`, `IDealTermsRenderer` and `IDealTermsSerializer` **no longer exist**. The
Concert module has been split into Application/Booking/Concert/Opportunity, and `DealTerms`
(`Deal.Contracts/DealTerms.cs`) is now an abstract record whose four arms each override `Render()` — the
keyed family was retired in favour of an abstract per-arm member.

**That is the in-repo precedent for this plan's direction**, and it is the shape to match wherever an arm is a
pure per-arm computation and the type is allowed to see its input.

The surviving `IDealStrategy` families are all genuine behaviour with collaborators and are explicitly **not**
targets: `IContractFactory` (Booking.Domain), `IDealPayeeResolver` and `ISettlementAmountResolver`
(Concert.Application).

## Design decisions

**A mapper is pure, total and dependency-free.** `entity → DTO` qualifies. `DTO → entity` can fail, so it is
construction, not mapping, and it lives in `DealFactory`, named for what it does.

**The entity keeps primitive-taking methods.** `FlatFeeDealEntity.Create(decimal, PaymentMethod)` and
`.Update(decimal, PaymentMethod)` already have the right shape. DTO unpacking is Application's job.

**`Apply` as an abstract member on `DealEntity` is rejected.** It is the nicer dispatch and would give
compiler-enforced exhaustiveness — `DealTerms.Render()` is exactly that shape — but `DealTerms` lives in
Contracts and may see a DTO, whereas `DealEntity` lives in Domain and, after Phase 1, may not. Correct layering
wins over the nicer dispatch.

**Entity→DTO correlation cannot be compiler-verified under dispatch, in any shape.** With `Deal.Domain`
unable to see `Deal.Contracts` and a DTO forbidden from mutating, neither hierarchy can be parameterized by
the other, so every candidate shape reduces to exactly one runtime-guarded seam. A generic template base —
`DealUpdater<TEntity, TDto>`, the `CollectionSyncer<TEntity, TDto>` shape — does not close that gap either:
its constraints are `TEntity : DealEntity` and `TDto : DealDto` independently, so `DealUpdater<FlatFeeDealEntity,
DoorSplitDealDto>` still compiles. What it does buy is real and is why it wins: the pair is declared once per
arm in type arguments, the downcast exists once in the base rather than once per arm, and the arm bodies are
one line each. `CollectionSyncer` gets its safety from a call site that knows the concrete syncer statically;
Deal has no such call site, so dispatch is unavoidable and the seam is priced, not removed.

**One dispatch point, and it is a `switch`, not a table.** `DealStrategyArchitectureTests.DealTypeFrozenDictionaries_AreAbsent`
bans `FrozenDictionary<DealType` across the Deal and Concert modules and still guards Concert's live keyed
families, so the `DealType` → arm dispatch is a private switch expression inside the updater. This is a
deliberate, recorded deviation from the `keyed-strategies` standard, whose answer for behaviour keyed by a
closed key is the validated keyed registry: that registry is what Phase 2 removes, because these arms carry no
collaborators and no behaviour beyond one `entity.Update(primitives)` call, so the composition ceremony bought
nothing. Exhaustiveness moves to a test, as already accepted below.

**Capability matching is not applicable.** A capability registry is a predicate for gating links without
instantiating a workflow. It answers yes/no; it does not dispatch to an implementation.

**Mapperly is adopted per file, not blanket.** It earns its keep where many members map by name —
`ITransactionMapper` (~8 properties × 3 arms). For `DealDto` (3–4 properties × 4 arms) a hand-written switch is
comparable in size and adds no dependency. `Riok.Mapperly` is already pinned at 4.3.1 (unused) on
`Chore/TestTierNaming` and sibling branches; reconcile rather than adding a second pin.

**`MemberVisibility.All` is used nowhere in this plan.** Mapping into private setters via `UnsafeAccessor`
bypasses the validating factory exactly as a public setter would, so it is not a DDD-preserving option. It is
also broken across assemblies on 4.3.1 (riok/mapperly#1458, fixed by #2139 merged 2026-02-04, absent from the
latest stable). The `entity → DTO` direction needs none of it: entity getters are public, DTO setters are
`init`.

## Phases

### Phase 1 — relocate the two enums, Deal layering fix

The two enums are not one concern and do not share a home.

- **`DealType` is Deal's own domain vocabulary** — the domain owns what kinds of deal
  exist; Contracts merely exposes that on the wire. It moves into `Concertable.B2B.Deal.Domain`, and
  `Deal.Contracts` takes a `ProjectReference` on `Deal.Domain` to reach it. `DealTypeNames` does **not** go
  with it: those are the JSON `$type` discriminator strings, so they live beside `DealDto` in Contracts. The dependency then points
  inward, which is the direction the layer graph wants.
- **`PaymentMethod` is not deal-specific** — 66 B2B files use it and nothing about it belongs to a deal. It
  moves to `api/src/Concertable.B2B.Enums` (`net10.0`), a service-local project: no
  cross-folder escape, so the standalone carve is unaffected and nothing needs publishing. It is B2B-only
  today; the Payment service's `PaymentMethod` hits are Stripe's own types, not this enum.
- `Deal.Domain`: drop the `ProjectReference` to `Deal.Contracts`, add one to `Concertable.B2B.Enums`.
- Add an architecture test asserting no B2B `*.Domain` project references a `*.Contracts` project, with the
  remaining modules on an explicit allowlist the test also asserts is still accurate.

**Consumption contract:** `Concertable.B2B.Enums` is the home for B2B enums that no single module owns. It
holds no DTOs, no interfaces and no behaviour. Consumers reference it by `ProjectReference` from inside
`api/`; nothing outside that folder may reference it. Enums a module *does* own live in that
module's Domain project.

**Known cost, deliberately accepted:** `Deal.Contracts → Deal.Domain` makes `Deal.Domain`'s public types
transitively visible to the 18 projects that reference `Deal.Contracts`, up from 6. The correct fix is the
visibility cascade — `internal` entities with `InternalsVisibleTo` — which was attempted and reverted: the
seed layer exposes `DealEntity` through public members (`SeedState.Deals`, the deal factories) that every
module's integration fixture consumes. Tracked as its own item rather than bundled here.

**Gate:** `api/Concertable.slnx` builds; `Concertable.B2B.Deal.UnitTests` green; the new layering test green
with Deal absent from the allowlist. Build/integration verification inherits `docs/REMOTE_VALIDATION.md`.

### Phase 2 — Deal mapper and updater collapse

- `DealMappers` — static, C# 14 extension members, `entity → DTO` only. Mapperly `[MapDerivedType]` over the
  four arms, plain `[Mapper]`, no visibility overrides.
- `DealDto.ToEntity()` — an abstract member per arm calling `XDealEntity.Create(primitives)`, so the
  DTO→entity create path needs no separate factory and the compiler enforces its exhaustiveness.
- `DealUpdater` — one file in `Deal.Application/Updaters`: a static `Update(DealEntity, DealDto)` entry point
  owning the deal-type mismatch guard, a private `DealType` → arm switch, an abstract
  `DealUpdater<TEntity, TDto>` template base holding the one downcast, and four private nested one-line arms
  calling `entity.Update(primitives)`. No DI, no async, no persistence; `DealUpdater` is the only name visible
  outside the file.
- Delete `IDealMapper`, `DealMapper` and the four arms; `IDealUpdater`, the `DealUpdater` facade and the four
  arms; and `DealStrategyFactoryExtensions`, left unreachable once the Deal module has no `IDealStrategy`.
- `DealService.Validate` becomes `deal.ToEntity()` matched down to a unit. Named honestly it is still
  construct-and-discard; lifting the per-arm guards out of the entities is explicitly **not** in scope.
  `DealService` loses its `IDealUpdater` dependency and is left holding only `IDealRepository`.
- Remove both `RequireAll` rows and the keyed registration blocks in
  `Deal.Infrastructure/Extensions/ServiceCollectionExtensions.cs`.
- Delete `DealStrategyFactoryTests` outright — with no Deal `IDealStrategy` left it has nothing to assert —
  drop `KeyedRegistrations_CoverEveryStrategyFamilyAndDealTypeExactlyOnce` from `DealStrategyArchitectureTests`,
  and repoint `DealStrategyBuilderTests` onto `new DealStrategyBuilder(services)` so no production member
  survives only to serve a test.
- Add `DealUpdaterTests` asserting a write per `DealType`, that the theory covers every `DealType`, and that a
  mismatched type and invalid terms both fail and leave the entity untouched. `DealMapperTests` already carries
  the equivalent guard for the mapper and `ToEntity`.

**Consumption contract:** `IDealService` and `IDealModule` keep their current signatures; no caller outside the
Deal module changes.

**Gate:** build; `Concertable.B2B.Deal.UnitTests`; `Concertable.B2B.Deal.IntegrationTests`.

**Known regression accepted:** adding a `DealType` member now fails a test rather than composition.
`DealStrategyArchitectureTests` already cross-checks the DTO arms, entity arms, enum members, JSON
discriminators and the TypeScript union, so the uncovered gap is narrow — an arm present everywhere but missing
from a switch — and the new exhaustiveness test closes it.

### Phase 3 — Payment and User

- `ITransactionMapper` → one static Mapperly mapper. Widest arms in the codebase; this is where Mapperly
  clearly wins. Requires the `Riok.Mapperly` pin in `api/Concertable.Payment/Directory.Packages.props`.
- `IUserMapper` → deleted outright. No dependencies, no Mapperly needed, and the `Task.FromResult` wrapper goes
  with it.

**Gate:** build; `Concertable.Payment.UnitTests` (`TransactionMapperTests` rewritten against the static
mapper); Payment integration suite.

## Explicitly out of scope

- **The remaining 11 `*.Domain → *.Contracts` references.** Same template, own roadmap item; doing all twelve
  up front turns a contained change into a multi-week one.
- **Moving vocabulary into `Concertable.Contracts`.** It is a published package, so it is a breaking
  published-contract change requiring its own expand/contract plan and multiple merges.
- **Moving `fee > 0` and the other guards to the API boundary.** Only needed for a bidirectional DTO↔entity
  mapper, which this plan rejects.
- **The genuine keyed families.** `IContractFactory`, `IDealPayeeResolver` and `ISettlementAmountResolver`
  carry real behaviour and collaborators; they stay keyed.

## Completion conditions

- No B2B `*.Domain` project references a `*.Contracts` project except those on the declared allowlist.
- `Deal.Domain` cannot reference `DealDto`; the build proves it.
- `IDealMapper`, `IDealUpdater`, `ITransactionMapper` and `IUserMapper` no longer exist.
- Every surviving mapper is static, total and dependency-free.
- Exhaustiveness over `DealType` is asserted by test everywhere `RequireAll` previously guaranteed it.
