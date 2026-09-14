# Layering and mapper hygiene roadmap

Two related defects surfaced together: `*.Domain` projects reference `*.Contracts` projects (so a domain
entity can legally take a wire DTO), and several "mapper" families are keyed-DI strategies whose arms carry no
dependencies and no behaviour. The first permits the second to look reasonable.

The epic ships when no B2B domain project can see a DTO and every surviving mapper is static, total and
dependency-free.

## Items

- [ ] Deal layering and DI-mapper collapse — `layering/deal-layering-and-mapper-collapse`.
  Relocates the two shared enums, cuts `Deal.Domain`'s reference to `Deal.Contracts`, and
  collapses the `IDealMapper`, `IDealUpdater`, `ITransactionMapper` and `IUserMapper` families. Deal is the
  proving ground for the template the remaining modules follow.

  `DealTerms` in `Deal.Contracts` is the epic's reference shape: an abstract record whose four arms each
  override `Render()`, having replaced a keyed `IDealTerms` family. Match it wherever an arm is a pure per-arm
  computation and the owning type is permitted to see its input.

- [ ] Remaining `*.Domain` → `*.Contracts` references — `layering/remaining-domain-contracts-references`.
  The other 11 modules (Admin, Artist, Concert, Conversations, Tenant, User, Venue in B2B; Messaging; Payment;
  Customer.Preference) follow the same template. Blocked on the item above only for the template, not for
  design. Concert.Domain is the awkward one: it reaches across modules for `Deal.Contracts` as well as its own.

- [ ] Shared enums to the published contract — `layering/enums-to-shared-contracts`.
  `Concertable.Contracts` already holds `Genre` and is the conceptually right long-term home for shared
  vocabulary. It is a **published package**, so this is a breaking published-contract change: it needs its own
  expand/contract design and cannot land in one PR. Deliberately deferred behind the two items above so a
  service-local project unblocks them first.

- [ ] Tighten domain type visibility — `layering/domain-visibility-cascade`.
  `Deal.Domain`'s entities are `public` when the module-structure standard wants `internal` with
  `InternalsVisibleTo`. Making them internal was attempted during the item above and reverted: the seed layer
  exposes `DealEntity` through public members (`SeedState.Deals`, `DealFactory`, `ApplicationFactory`) that
  every module's integration fixture consumes, so the change cascades through `Seed.Infrastructure` and the
  fixtures. Until this lands, `Deal.Contracts -> Deal.Domain` leaks the entities to 18 projects.

- [ ] Rename the two genuine-DI application mappers — `layering/application-mapper-renames`.
  `Concert.Api/ApplicationMapper` injects `IConcertWorkflowCapabilityRegistry` to gate HATEOAS links — it is a
  presenter. `Concert.Application/ApplicationMapper` injects `IContractRepository` and batches to avoid an
  N+1 — it is an assembler. Both are correctly dependency-injected and simply misnamed; neither is a mapper.
  Independent of the other items.

## Cross-item ordering

The vocabulary project introduced by the first item is the consumption contract for the second. The third
supersedes the location chosen by the first, and must not start until the second is terminal — otherwise the
remaining modules migrate twice.
