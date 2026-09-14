# Stage snapshots — recovered from the monorepo archive

Both files here are recovered verbatim from the archived monorepo and exist on no branch tip.

| File | Was at | Removed by |
|---|---|---|
| `ORGANIZATION_REFACTOR_PLAN.md` (346 lines, blob `237ca59b`) | `plans/ORGANIZATION_REFACTOR_PLAN.md` | `ace0e8ff0` "drop superseded Organization refactor" |
| `PLATFORM_FEE_STORAGE_INVESTIGATION.md` (387 lines, blob `d6a7b659`) | `plans/b2b/PLATFORM_FEE_STORAGE_INVESTIGATION.md` | never tracked to a tip after `48e4f6fcb` |

## Why this matters — the snapshot design was collateral damage

`ORGANIZATION_REFACTOR_PLAN.md` was binned because its **Organization aggregate** framing was superseded:
B2B settled on `Tenant` as the canonical term, with no separate organisation aggregate (see `AGENTS.md`).
That supersession is correct and stands.

But the plan's **Phase 4 — Snapshot ComplianceContext onto BookingEntity** is a *different* concern that
went out with it, and it was never re-homed:

> Snapshot is taken **at Accept** (when `ApplicationEntity` transitions to `BookingEntity`), not at
> Opportunity creation. Rationale: the Application is speculative; the Booking is binding. At settlement
> time, workflow steps read `booking.VenueCompliance` and `booking.ArtistCompliance`, **never** the
> current state of `OrganizationEntity`.

That is database snapshotting at a stage transition: owned-type value objects frozen onto the row when the
aggregate advances. Read `Organization` as `Tenant` throughout and the design still applies.

## It did not land, and the gap is live

`ComplianceContext` and `PlatformFeeSnapshot` do not exist anywhere in this repo. `BookingEntity` takes an
`ApplicationAcceptanceSnapshot`, so the **in-process** handoff exists, but nothing compliance-related or
fee-related is frozen onto the row.

The consequence is observable today in
`Concert.Infrastructure/Services/Settlement/SettlementService.cs`:

```csharp
var supplierComplete = await tenantModule.IsTaxComplianceCompleteAsync(supplierTenantId);
var customerComplete = await tenantModule.IsTaxComplianceCompleteAsync(customerTenantId);
```

Settlement reads **current** tax-compliance state, which is exactly what the plan forbids. The plan's own
stated risk applies: *"snapshot must include every field needed by settlement and DAC7. If we miss one, we
re-read current state and lose the audit guarantee."* Right now every field is missed, because there is no
snapshot at all.

`PLATFORM_FEE_STORAGE_INVESTIGATION.md` is the follow-up that re-examines whether the per-row fee snapshot
is a smell — read it before implementing, because it may revise Phase 4's `PlatformFeeSnapshot` decision.

## Relationship to other plans

- `booking-agreement-snapshot/` is the *terms* snapshot at the same Accept boundary; this is the
  *compliance and fee* snapshot at that boundary. Same transition, different payload.
- `DEAL_CONFIGURATION_PLAN.md` Phase 2 pins a configuration revision into the accepted Contract — a third
  payload frozen at the same point. All three want one coherent Accept-time snapshot story.

## Open question — is carrying the snapshot by value the right design at all?

Raised 2026-09-14, deliberately unresolved. Record it here rather than lose it; settle it when the
snapshot design is revisited.

The position: the large by-value snapshots are a *consequence of the modular monolith*, not of the
domain. Two facts make that concrete.

**Sealing removes the correctness reason for copying.** Today a consumer must copy because the source
row stays writable after the stage advances, so a reference could silently change. Once
`LIFECYCLE_SEAL_ENFORCEMENT_PLAN.md` lands, a reference to a sealed row is exactly as trustworthy as a
copy of it.

**The boundary is code discipline, not physics.** Every B2B module context binds the same `B2BDb.Name`
connection — Application, Booking and Concert rows are in one database, sharing one migrations history.
There is no network hop and no independent availability between them. The prohibition on reading across
a module ("no cross-module queries even from a read stance") is an architectural rule this service
chose, not a constraint the deployment imposes.

So after sealing, the remaining justification for a by-value snapshot is the module rule itself. That is
a real and defensible rule, but it should be argued on its merits rather than treated as forced.

### What sealing would and would not shrink

| Payload | Effect |
|---|---|
| `ApplicationAcceptanceSnapshot` (~25 fields over 6 nested records) | the real target — it is defensive breadth, carried because re-reading is unsafe today |
| `ConfirmedBookingSnapshot` (12 fields) | measured: Concert consumes all 12, none carried speculatively. Little to win |
| `ContractEntity.VenueName` / `ArtistName`, `TermsText`, terms versions | **not** shrinkable. These are point-in-time legal values — the document must say what the parties were called then. A sealed row and a frozen display value are different requirements |

### Do not conclude from this

That the snapshots are waste. They are correct under today's rules. The question is whether the rules
should change once sealing removes their main justification — and that is a deliberate architectural
decision, not a cleanup.
