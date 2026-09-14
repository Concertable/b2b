# Booking-agreement snapshot v2 — freeze the typed contract, not flattened columns

> Follow-up quality refactor to the shipped booking-agreement feature (LEGAL item 2). **Internal to
> the B2B Concert module — no published-contract change, so it's a single PR** (unlike the
> `IPdfRenderer` rename). Do it after `Feature/BookingAgreement` (#90) merges, on a
> `Refactor/BookingAgreementSnapshot` branch.

## Problem

`BookingAgreementEntity` snapshots the agreed terms by flattening the contract into a parallel
nullable union of columns — `ContractType`, `PaymentMethod`, `Fee?`, `HireFee?`, `Guarantee?`,
`ArtistDoorPercent?`. This **duplicates the TPT `ContractEntity` shape** as loose columns on a second
entity: every time a contract type gains/changes a field, two places must change, and the agreement
carries a bag of mostly-null columns. It reads as "the contract dumped into a random entity."

The agreement entity itself is **correct** and stays — it's the executed-deal legal record (parties,
consent blocks, platform-terms version, PDF), a distinct concern from the mutable, shared *offer*
`ContractEntity`. Only the *terms-snapshot mechanism* is being reworked.

## Constraint that shapes the design

- The offer `ContractEntity` (TPT) lives in the **Contract module** and is **shared + mutable** across
  every applicant to an opportunity. It can't be frozen in place at Accept (that would freeze it for
  other applicants / block the venue editing the opportunity), and the Concert module must not FK to
  it — modules communicate via interfaces, not shared tables (`docs/MODULAR_MONOLITH_RULES.md`).
- What Concert *does* have at Accept is the resolved `IContract` value (`IContractAccessor.Contract`)
  handed across the module boundary — the same polymorphic shape used everywhere. The snapshot must be
  built from that, self-contained in the Concert context.
- Drift is already handled: the terms-fingerprint guard 400s the Accept if the contract changed since
  Apply, so "what both parties agreed to" is already pinned. This refactor is about *representation*,
  not integrity.

## Options

- **A — flat scalar columns (status quo).** Precedent-consistent (Customer `TicketEntity` purchase
  snapshot), queryable, but duplicates the TPT shape as a nullable union. The smell.
- **B — EF owned typed snapshot.** Owned value object on the agreement. EF owned types don't model
  polymorphism well, so it collapses back to per-type nullable columns — relocates the smell without
  removing it. Rejected.
- **C — JSON snapshot of the polymorphic `IContract` (recommended).** One `ContractSnapshot` column
  holding the serialized `IContract` (System.Text.Json polymorphic, type-discriminated). Write-once,
  read-whole (PDF + metadata DTO), never queried by term — the ideal JSON-column case. Eliminates the
  duplicate columns; "the exact typed contract, frozen." Cost: introduces a JSON column (the v1 plan
  deliberately had "no JSON precedent") — justified by the write-once/read-whole access pattern.

## Decision

**C.** Replace the flattened columns with a single JSON `ContractSnapshot` of the agreed `IContract`.
Keep the rendered human-readable `TermsText` (legal display string) and everything else on the
agreement unchanged.

## Phases (single PR)

### Phase 1 — Add the snapshot alongside
- Add a `ContractSnapshot` (serialized `IContract`, polymorphic) to `BookingAgreementEntity`;
  populate it in `BookingAgreementBuilder` from `contractAccessor.Contract`. Leave the flat columns in
  place for now so nothing breaks.

### Phase 2 — Move readers onto the snapshot
- `BookingAgreementDto` / `BookingAgreementDocument` (PDF) read terms from `ContractSnapshot` instead
  of the flat columns. Adjust the `BookingAgreementApiTests` term assertions to read the snapshot.

### Phase 3 — Remove the flat columns
- Delete `ContractType`/`PaymentMethod`/`Fee?`/`HireFee?`/`Guarantee?`/`ArtistDoorPercent?` from the
  entity + config; re-scaffold migrations (`./initial-migrations.ps1`). `git rm` this plan.

## Gate

- Solution build · Concert integration via `integration-debug` (the snapshot / survives-contract-edit
  / download tests still pass) · `./initial-migrations.ps1` · four web builds **if** the agreement
  metadata DTO shape changes (check the FE `getAgreement`/download consumers — currently the FE only
  uses the PDF download + HATEOAS link, so likely no FE change, but verify).

## Out of scope

- Making the offer `ContractEntity` itself immutable / versioned, or blocking opportunity edits while
  applications are pending — a separate, larger product decision; the fingerprint guard already covers
  the legal risk. This plan only changes how the agreement *stores* its frozen terms.
