# Deal layering and DI-mapper collapse progress

- Plan: `docs/plans/layering/DEAL_LAYERING_AND_MAPPER_COLLAPSE_PLAN.md`
- Roadmap: `docs/plans/layering/LAYERING_ROADMAP.md`
- Roadmap item: `layering/deal-layering-and-mapper-collapse`
- Worktree: `C:\Users\TommySeery\source\repos\Concertable\.worktrees\Refactor-layering_deal-vocabulary-and-mapper-collapse`
- Branch: `Refactor/layering_deal-vocabulary-and-mapper-collapse`, based on `origin/main` @ `15ce7946f`
  (the branch and worktree keep the original name; renaming them is blocked while commits are unpushed, and
  the PR title is what matters)
- PR: none yet
- Dependency/package gates: `Riok.Mapperly` 4.3.1 was already pinned in `api/Directory.Packages.props`
  and is now referenced by `Deal.Application`. Phase 3 needs the same pin in
  `api/Concertable.Payment/Directory.Packages.props`.
- Last reconciled: 2026-09-08 against `origin/main` @ `15ce7946f` in this worktree

## Current state

**Phases 1 and 2 complete.** Both Deal families are gone and the module holds no `IDealStrategy` at all.
Phase 3 (Payment `ITransactionMapper`, B2B `IUserMapper`) is the only remaining work and is independent.

The plan was originally drafted against `Docs/launch_seal-and-postgres-plans`, whose Concert module predates
the Application/Booking/Opportunity split. Every file path and family roster in the plan has since been
re-verified against `origin/main` @ `15ce7946f`.

## Completed milestones

- **Phase 1** — `DealType` moved into `Concertable.B2B.Deal.Domain`;
  `PaymentMethod` moved into the new service-local `Concertable.B2B.Enums`, registered in
  `api/Concertable.slnx`; `Deal.Domain` repointed off `Deal.Contracts` onto `Concertable.B2B.Enums`;
  `Deal.Contracts` takes a reference on `Deal.Domain` for `DealType`; 51 files repointed off the old
  namespace; `LayeringArchitectureTests` added asserting the Domain→Contracts violations are exactly the ten
  modules still pending.

  The first cut of this (`3d4fa7ab4`) put both enums in one service-level `Concertable.B2B.Vocabulary`
  project. Rejected: `DealType` is Deal's own domain vocabulary and belongs in `Deal.Domain`, and the name
  was not carrying its weight. Do not reintroduce a single shared home for both enums.

- **Phase 2a** — `IDealMapper`, the `DealMapper` facade and its four arms deleted (6 files). Replaced by
  `Mappers/DealMapper.cs` (Mapperly `[Mapper]`, `[MapDerivedType]` over the four pairs, generating `ToDto`
  and `ToDtos`). `ToEntity` is an abstract member on `DealDto` overridden one line per arm, so the DTO→entity
  switch is gone too and no extension class survives.
  `DealService` drops the `IDealMapper` dependency; the keyed `IDealMapper` registrations and the matching
  rows in `DealStrategyArchitectureTests` and `DealStrategyFactoryTests` are gone. New `DealMapperTests`
  covers both directions per `DealType`, which is also the exhaustiveness guard the `RequireAll` row used
  to provide.

  `Riok.Mapperly` was already pinned at 4.3.1 in `api/Directory.Packages.props`; only the
  `PackageReference` on `Deal.Application` was needed. `[MapperIgnoreSource(nameof(DealEntity.TenantId))]`
  declares the one deliberate source-only member — tenancy is ambient and absent from the DTO.

- **Phase 2b** — `IDealUpdater`, the `DealUpdater` facade and its four arms deleted (6 files), replaced by
  `Deal.Application/Updaters/DealUpdater.cs`: a static `Update(DealEntity, DealDto)` entry point owning the
  deal-type mismatch guard, a private `DealType` → arm switch, an abstract `DealUpdater<TEntity, TDto>`
  template base holding the single downcast, and four private nested one-line arms. `DealUpdater` is the only
  name the file exposes. `DealService` drops the dependency and now holds only `IDealRepository`; the keyed
  registrations, both `AddDealStrategies` overloads and `RequireAll<IDealUpdater>()` are gone from
  `Deal.Infrastructure`.

  `DealStrategyFactoryExtensions` went with them — its `Create(DealDto)` / `Create(DealEntity)` overloads had
  no callers left once the `IDealMapper` facade was deleted in `1b70cd0b8`, and the Deal module now has no
  `IDealStrategy` implementation at all. `Deal.Infrastructure` no longer registers `IDealStrategyFactory<>`;
  every module that consumes it (Application, Booking, Concert) registers it through its own
  `AddXDealStrategies` and `AddDealStrategyFactory` uses `TryAdd`, so nothing lost a registration.

  `DealStrategyFactoryTests` was deleted rather than rewritten, `KeyedRegistrations_CoverEveryStrategyFamilyAndDealTypeExactlyOnce`
  dropped from `DealStrategyArchitectureTests`, and `DealStrategyBuilderTests` repointed onto
  `new DealStrategyBuilder(services)` — mirroring `DealUnionBuilderTests` — so no production member survives
  only to serve a test. New `DealUpdaterTests` covers a write per `DealType`, `DealType` coverage of the
  theory, and the mismatch and invalid-terms failures.

## Latest verification

- `Concertable.B2B.Deal.UnitTests` builds clean and passes **57/57**, which transitively proves
  Deal.Contracts, Deal.Domain, Deal.Application and Deal.Infrastructure compile. (58 before Phase 2b: the
  seven `DealStrategyFactoryTests` cases and one architecture fact went, seven `DealUpdaterTests` cases came.)
- `Concertable.B2B.Deal.Api` and `Concertable.B2B.Concert.UnitTests` build clean, and Concert passes
  **105/105** — the other consumer of the shared `DealStrategyFactory` / `DealStrategyBuilder` machinery,
  which this phase deliberately leaves in place.
- **`LayeringArchitectureTests` has never been executed.** `Concertable.B2B.ArchitectureTests` references the
  composition root, which does not build — see the blocker below. PR CI is its first real run; expect to fix
  the pending list there if it is wrong.

## Blocker on the full-solution gate (pre-existing, not caused by this branch)

`api/Concertable.slnx` does not build. `Concertable.B2B.Application.Infrastructure/Services/ApplicationCheckoutService.cs:112`
calls `IEscrowOperationsClient.AuthorizeAsync`, which the pinned `Concertable.Payment.Client` package does not
expose. **Verified pre-existing:** it reproduces at `origin/main` @ `15ce7946f` with this branch's changes
stashed. This is the recorded debt that PR CI never builds against the pinned packages (`1d11deec6`).

Consequence for this plan: the phase gates fall back to the smallest affected project plus its unit tests, and
anything requiring the composition root — the architecture suite included — is gated on PR CI rather than
locally.

## Decisions and discoveries that affect execution

- **Two of the original targets no longer exist on main.** `IPaymentAmountMapper`, `IDealTerms`,
  `IDealTermsRenderer` and `IDealTermsSerializer` are gone. The old Phase 3 was deleted; scope dropped from
  ~24 files to 17.
- **`DealTerms` (`Deal.Contracts/DealTerms.cs`) is the reference shape** — an abstract record whose four arms
  each override `Render()`, replacing a keyed family. Match it where an arm is a pure per-arm computation.
- **`Deal.Domain` uses only two symbols from `Deal.Contracts`** — `DealType` and `PaymentMethod`, namespace
  `Concertable.B2B.Deal.Contracts.Enums`. No DTO usage exists. The defect is that the assembly-wide reference
  *permits* it, not that it is currently exploited.
- **`Deal.Contracts` is not packable** (B2B `Directory.Build.props:39`), so the enums need no domain/wire
  duplication. This was the expensive-looking option and it is not required.
- **`Concertable.Contracts` is the wrong home for now.** It already holds `Genre` and is conceptually right,
  but it is a published package and `plans/AGENTS.md` forbids a published-contract change landing in one PR.
- **Mapperly `MemberVisibility.All` is a dead end here** and is used nowhere in the plan. It bypasses the
  validating factory exactly as a public setter would, and riok/mapperly#1458 (private members across
  compilation units) is fixed only by #2139, merged 2026-02-04 and absent from the latest stable 4.3.1
  (published 2025-12-22). The `entity → DTO` direction never needed it.
- **Nothing in this plan is waiting on a Mapperly release.** The rejection above is a design decision, not an
  availability one: a Mapperly 5 shipping `MemberVisibility.All` across assemblies would still not be used,
  because after Phase 1 `Deal.Domain` cannot reference `DealDto` at all. Do not revisit this plan when 5.0
  lands. The only deferred choice is the vocabulary's *location*, gated on the published-package rule and
  tracked as `layering/enums-to-shared-contracts`.
- **`Apply` as an abstract member on `DealEntity` was designed and rejected.** `DealTerms.Render()` proves the
  shape works in this codebase, but `DealTerms` lives in Contracts and may see a DTO; `DealEntity` lives in
  Domain and, after Phase 1, may not. Do not re-propose it.
- **The entity↔DTO correlation cannot be made compiler-visible under dispatch. This is settled — stop
  looking.** Domain cannot see Contracts and a DTO may not mutate, so neither hierarchy can be parameterized
  by the other and every shape reduces to one runtime-guarded seam. The generic template base does **not**
  close it: `TEntity : DealEntity` and `TDto : DealDto` are independent constraints, so
  `DealUpdater<FlatFeeDealEntity, DoorSplitDealDto>` compiles. `CollectionSyncer<TEntity, TDto>` is safe only
  because `OpportunitySyncer`'s call site knows the concrete syncer statically; Deal's call site holds an
  abstract `DealDto`. What the base genuinely buys — and why it was still the right shape — is the pair
  declared once per arm in type arguments, the downcast written once instead of four times, and one-line arms.
- **The `DealType` → arm dispatch is a switch, never a table.**
  `DealStrategyArchitectureTests.DealTypeFrozenDictionaries_AreAbsent` bans `FrozenDictionary<DealType` across
  the Deal and Concert modules and still guards Concert's live keyed families; weakening it to hold four
  stateless Deal arms would have been the wrong trade. A private switch expression inside `DealUpdater` needs
  no change to that guard. The FSM design ruling ("NO frozen table") points the same way.
- **Deviation from `keyed-strategies`, deliberate and recorded.** That standard's answer for behaviour keyed by
  a closed key is the validated keyed registry — exactly what Phase 2 removes. It is removed because these arms
  carry no collaborators and no behaviour beyond one `entity.Update(primitives)` call, so composition-time
  coverage was being bought with a six-file DI family. Coverage is now a unit test. Reviewers should read this
  as an intended exception, not an oversight.
- **`DealDto.ToEntity()` stays the create path.** It was deliberately not folded into the updater arms even
  though one `DealType` → arm dispatch point could serve both: virtual dispatch on the DTO gives
  compiler-enforced exhaustiveness that the switch cannot, so create keeps the stronger guarantee and only
  update pays for the seam.
- **The surviving keyed families are not targets.** `IContractFactory` (Booking.Domain),
  `IDealPayeeResolver` and `ISettlementAmountResolver` (Concert.Application) carry real behaviour.

## Reviews

None yet.

## Next Steps

**Phase 3 — Payment `ITransactionMapper` and B2B `IUserMapper`.** Independent of everything above.
`ITransactionMapper` (interface, facade, three arms in `Payment.Application/Mappers`) becomes one static
Mapperly mapper, which needs `Riok.Mapperly` pinned in `api/Concertable.Payment/Directory.Packages.props`;
`TransactionMapperTests` is rewritten against it. `IUserMapper` (`B2B User.Infrastructure/Mappers`) is deleted
outright — no dependencies, and its `Task.FromResult` wrapper goes with it. Gate on
`Concertable.Payment.UnitTests` plus the Payment integration suite.

**Then open the PR.** Nothing before Phase 3 needs the composition root, and the branch has never been pushed;
`LayeringArchitectureTests` gets its first real execution in PR CI, so expect to correct the pending-module
allowlist there.
