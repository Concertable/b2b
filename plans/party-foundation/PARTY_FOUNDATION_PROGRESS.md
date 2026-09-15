# Party foundation progress

- Plan: `plans/party-foundation/PARTY_FOUNDATION_PLAN.md`
- Roadmap: `plans/party-foundation/PARTY_FOUNDATION_ROADMAP.md`
- Roadmap item: `party-foundation/core`
- Worktree: `C:/Users/TommySeery/source/repos/Concertable/b2b/.worktrees/Refactor-PartyFoundationLegacyBindings`
- Branch: `Refactor/PartyFoundationLegacyBindings`
- Base: cached origin/main `2b5264b4`
- PR: none; local planning work only.
- Dependency/package gates: vocabulary/claim owner before overlapping delivery; P2 Concert/Seed/Hosting publication and Customer/Search bumps; Postgres only gates configuration persistence.
- Last reconciled: 2026-09-15 after the branch restart; runtime tree identical to origin/main `2b5264b4`, live PR #14/#15 heads checked.

## Current state

The rejected plan has been replaced end to end. The new plan starts with resource access across all
nine pair-scoped entities, then accepted participants/Show, signing representation, direct invitation
and an evidence approval that actually gates publication. It contains the model, contracts, schema,
transaction/revocation rules, consumer chain and dispositions for all 32 root debt entries.

The branch was then restarted. The two rejected implementation/remediation commits and the rejection
brief were dropped; nothing had been pushed. Every `api/` path is identical to origin/main, so
`LegacyFinancialParties` does not exist and main's `IDealPayeeResolver` keyed family is live again.
The branch carries documentation only, and no foundation phase is complete.

## Next Steps

On the next implementation request, execute P1 from the rewritten plan against the restarted branch,
then continue within the scope below. Do not restart the replan, recover the dropped commits, or
execute the rejected adapter-first sequence.

Scope: whole plan through all remaining phases and terminal delivery.
Current slice: P1 — replace pair access and enable restricted third-business participation.
Remaining scope: P2 accepted participants/Show/payment correlation and consumer closure; P3 representation; P4 direct invitation; P5 enforced evidence approval and terminal qualification.
Done when: P1–P5 consumption/verification gates pass, all required producer/consumer delivery legs and handoffs close, and the plan's terminal closeout is complete.

1. Recheck this checkout and the live sibling owners before edits. Consume the exact vocabulary,
   OperationClaim and AttemptVerdict source from `Refactor/DealVocabularyAndMapperCollapse`;
   it already implements the claim/mapping debt. Resolve overlaps locally without editing its dirty
   TECH_DEBT.md or the backlog owner's generated files. Coordinate delivery order with the owner.
2. Implement the P1 grant/permission/system-scope contract across Application, Booking, Contract,
   Concert, Invoice, availability, Message, ThreadReadState and ContentReport. Replace normal/read/
   privileged query, projection, download, notification and mutation paths together. Include authoritative
   membership versions and the shared transaction/receipt/outbound-intent boundary for current writes.
3. Add Tenant contact/eligibility facts, zero-or-more profiles, neutral shared/web/mobile membership and
   operations flows, typed resource sharing/revocation, Thread grants and per-member watermarks.
   Prove a distinct business can read an explicitly shared
   concert summary and cannot see fees, contracts, invoices, private messages or financial actions.
4. Replace the pair-derived financial direction where its live consumers actually read it — main's
   `IDealPayeeResolver`/`DealPayeeResolver` family in Concert.Application and its two directional
   strategies. Do not reintroduce a `LegacyFinancialParties`-style value. Update Concert/AGENTS.md,
   Deal/ARCHITECTURE.md and Deal/LEGAL_REQUIREMENTS.md, which currently describe that family
   correctly, to match what P1 implements. Keep InvoiceParty's reservation; new domain types use
   Participant.
5. Regenerate affected InitialCreate/snapshots and synthetic fixtures; run focused builds/tests and
   real-provider access/fence checks. Review a committed candidate. Current PR CI does not run Api/Ui
   E2E: obtain exact-commit evidence through e2e.yml for changed browser journeys; run existing web/mobile
   builds and qualify native tenant navigation/switching separately.
6. Deliver the qualified P1 candidate, checkpoint the actual state, and continue into P2. P2 must replace
   fixed accepted financial identities and close the Concert/Seed/Hosting producer → Customer/Search
   published-package chain; no adapters, old schema readers or invented backfills.

## Completed work

- This planning change: full replacement design, five implementation phases, exhaustive debt
  disposition, current sibling/consumer evidence and concrete first implementation slice.
- Review corrections: P1 fences/contact/all clients; intermediate consent with separate content seals;
  authoritative Opportunity slots and performer/location handoffs; durable financial-record contracts.
- Branch restart: the rejected implementation/remediation commits and the rejection brief were dropped
  rather than superseded inside P1, because P1's scope bears no resemblance to them and deleted their
  value anyway. Their only trace is this checkout's reflog; treat them as gone.

## Verification

- Planning graph: zero errors/warnings. Local file/heading links and git diff --check pass.
- Packaged repository-state provider validates this plan, ledger, branch/worktree and P1 next action.
- The 533 unit/architecture passes and green build belonged to the discarded runtime commits and are
  not evidence for any phase; no runtime test, migration, Docker or deployment evidence is claimed.
- Actual CI inventory: ci.yml runs Unit/Integration/Architecture/Startup and migration/package checks;
  e2e.yml runs separately on schedule/manual dispatch.

## Reviews

- Rewritten plan: independent native/general and contract/followability review, with parent validation
  and remediation. Findings, dispositions and the current committed watermark are in the canonical artifact
  `reviews/Refactor-PartyFoundationLegacyBindings.md` ([work order](../../reviews/Refactor-PartyFoundationLegacyBindings.md)).
- The prior runtime review pass is void: the branch restart discarded its candidate. Its F10 supplied
  the Participant decision recorded below; this documentation review grants no runtime approval.

## Decisions, discoveries, blockers, and deviations

- Nothing is live. Replace model/DTO/message shapes and regenerate synthetic data; no compatibility,
  backfill or retained-data delivery phase.
- New relationship vocabulary is Participant. Initial settlement binds only payer/payee; agency receipt,
  extra financial legs and paid amendments remain separate capabilities.
- `acd4a6ed` contains real OperationClaim and in-process attempt classification, not a persisted attempt
  journal. Its old progress ledgers still carry archived PR/path and “unimplemented” claims.
- PR [#14](https://github.com/Concertable/b2b/pull/14): local `d2613b7d`, remote `c4e1e88e`;
  human erasure/HasLiveObligationsAsync and shared ActionLink overlap foundation paths.
- PR [#15](https://github.com/Concertable/b2b/pull/15): `c5993e49`, required ci-complete context repair.
- Customer/Search actual semantic publication dependency is ConcertChangedEvent; Seed/Hosting closure
  also compiles against it. Search's Tenant.Contracts use is PayoutOwnerRegisteredEvent, which has no
  TenantType. Remove profile-derived ticket PayeeUserId; map TicketSellerTenantId to Payment PayeeOwnerId.

### Outgoing central-owner corrections

Central docs is owned by `C:/Users/TommySeery/source/repos/Concertable/docs`,
branch `Docs/PartyModelPromptEvidence`, inspected head `59a16f6d`. No sibling files were edited here.

| Owning artifact | Required correction / return condition |
|---|---|
| product/CONFIGURATION_EXPRESSIVENESS.md and CONFIGURABLE_DEAL_WORKFLOWS.md | Replace rejected adapter/backfill/phase references with this active plan and Participant vocabulary; keep product authority/disclosure invariants |
| plans/launch/DEAL_CONFIGURATION_PROGRESS.md | Replace old Docs/PartyFoundation worktree and legacy fixtures/phase gates. Consume P1 access, P2 accepted bindings, P3 representation, P4 both routes and P5 enforced approval when their exact artifacts arrive |
| plans/launch/POSTGRES_MIGRATION_PROGRESS.md and plan | Remove stale “P1 uncommitted” observation; second schema owner rebases/regenerates and reruns token/index/seal/authority-race tests against the first |
| Recovered commercial RECOVERY / central Deal Configuration | Explicitly reconcile D29 relational TPH/no mandatory template revision with the selected hybrid graph/revision chain before configuration persistence. Foundation adopts AgreementSnapshot/typed origins and does not silently settle that economic-storage conflict |

These are owner handoffs, not missing design inputs blocking P1. Track their delivery at the next
substantive central-owner checkpoint; do not treat stale copies in other worktrees as this ledger.
