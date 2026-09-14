# Recovered from the monorepo archive — read this first

The three documents beside this file come from the archived monorepo branch
`Feature/launch_gdpr-subject-rights` (tip `33fc0cab7`, 8 commits, last dated 2026-08-22), where they lived
under `plans/launch/`. Draft PR [#707](https://github.com/Concertable/concertable/pull/707) can never
merge — `Concertable/concertable` is archived and read-only. The code they describe was ported into `b2b`
on 2026-09-14 on `Feature/MonorepoBacklogPort`.

## The pause is resolved, and the reason was misreported

The branch's last commit paused Phase 1 because the Concert-side reads were built against the
**monolithic** Concert module that `launch/deal-lifecycle-ownership` (the PR #633 chain) was mid-way
through decomposing. That carve has since landed. It is **not** the same thing as the deal-vocabulary /
mapper-collapse work, which a later handoff note confused it with.

Post-carve entity homes, verified on `main` before porting:

| Entity | Module |
|---|---|
| `ApplicationEntity` | Application |
| `BookingEntity`, `ContractEntity` | Booking |
| `ConcertEntity`, `InvoiceEntity`, `SelfBillingAgreementEntity` | Concert |

## What was ported verbatim

The whole `Concertable.B2B.Privacy` module — `SubjectErasureRequestEntity` + `ErasureState` /
`ErasureTrigger` / `ErasureStateMachine` / `ErasureTransitionError`, `SubjectErasureService`,
`PrivacyDbContext` and its `InitialCreate` migration, seeders, `SubjectRightsController` — and the
User / Tenant / Conversations facade fragments, unit suite and integration suite. Paths were rewritten
`api/Concertable.B2B/src/` → `api/src/`, `api/Concertable.B2B/tests/` → `api/tests/`.

## What was rebuilt, not ported

**The Concert fragment, exactly as the pause commit instructed.** One `IConcertModule` obligation check plus
one records export became three:

- **Obligation check** — `IApplicationModule`, `IBookingModule` and `IConcertModule` each expose
  `GetLiveObligationCountAsync(tenantIds)`, and `SubjectObligationChecker` ORs them. Each module owns an
  internal `ObligationChecker` listing its own *settled* states explicitly and treating everything else as
  blocking, preserving the original's fail-closed default: Application settles on
  `Applied`/`Rejected`/`Withdrawn`/`Cancelled`; Booking only on `Cancelled`; Concert on
  `Draft`/`Complete`/`Cancelled`, plus a current self-billing agreement blocking regardless.
- **Records export** — `IConcertModule.GetRecordsExportAsync` keeps invoices + self-billing agreements;
  contracts moved to `IBookingModule.GetContractExportsAsync` with `ContractExport` re-homed into
  `Booking.Contracts`. `SubjectExporter` composes both.

`IConcertReadDbContext` gained only `Invoices`; the branch's `Applications` and `Contracts` additions would
now be cross-module queries and were not made.

## Three deviations forced by guards that postdate the branch

1. **Lifecycle facades expose `Get*` members only** (`ModuleBoundaryTests.LifecycleModuleFacades_ExposeQueryMembersOnly`).
   `HasLiveObligationsAsync` → `GetLiveObligationCountAsync` (returns a count the caller compares to zero,
   so it stays a published fact rather than a command); `ExportRecordsAsync` → `GetRecordsExportAsync`;
   `ExportContractsAsync` → `GetContractExportsAsync`.
2. **Reunion packages must be owned by their actual source consumers**
   (`ReunionTests.ReunionPackages_AreOwnedDirectlyByTheirSourceConsumers`). The branch's
   `Reunion.Errors` references on `Privacy.Application` and `Privacy.Infrastructure` are unused — dropped.
3. **`ServiceProviderScopeExtensions.RunScopedAsync`** was added on the branch to the in-tree shared
   `Concertable.Testing.Integration`. That is a published package from `platform-dotnet` now and is not
   editable from here, so the helper lives in `b2b`'s own `Concertable.B2B.IntegrationTests.Fixtures`.
   Logged in `TECH_DEBT.md` for promotion to the shared package.

## One genuine bug on the branch tip

`SubjectExporter` and `SubjectRightsApiTests` called `IUserModule.ExportAsync`, but the interface declares
`ExportUserAsync` — the branch tip does not compile. This is the residue of the naming rework its last
commit describes as "reverted to the last buildable commit"; it was not. Fixed to `ExportUserAsync`.

## Not yet proven

`SubjectRightsApiTests` has still never run anywhere — it is Testcontainers-gated and was written against
the pre-carve lifecycle. Its seeded-state assumptions are the first thing to re-confirm. See
`GDPR_SUBJECT_RIGHTS_PROGRESS.md` `## Next Steps`.
