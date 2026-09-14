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
