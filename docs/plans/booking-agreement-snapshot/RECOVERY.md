# Recovered from the monorepo archive — read this first

`BOOKING_AGREEMENT_SNAPSHOT_V2.md` beside this file is recovered verbatim from the archived monorepo.
It lived at `plans/b2b/BOOKING_AGREEMENT_SNAPSHOT_V2.md`, was added in `0ec8f1473` and removed in
`5867f8c14` — its own Phase 3 instructed `git rm` of the plan, so it deleted itself on completion and
survives on no branch tip. Blob `c273b376`, recoverable only from `Concertable-full-backup.bundle`.

## What of it landed

The representation change did, in a different shape. `BookingAgreementEntity` no longer exists.
`ContractSnapshot` is now a typed record in `Concertable.B2B.Application.Contracts`, nested inside
`ApplicationAcceptanceSnapshot`, carrying the polymorphic `DealTerms Terms` rather than the flattened
`Fee?`/`HireFee?`/`Guarantee?`/`ArtistDoorPercent?` nullable union the plan called a smell. It also
carries `TermsText`, `PlatformTermsVersion`, `MandateTermsVersion`, both `ContractSignature`s and the
`PaymentOperationReference` commitment.

## What did not land

**The storage half.** The plan's decision was option C — serialize the polymorphic contract into a JSON
column, write-once/read-whole. Today `ContractSnapshot` is an in-process record crossing the stage
boundary, not a persisted JSON document. That is the piece the Postgres cut-over unblocks, and it is the
same argument `DEAL_CONFIGURATION_PLAN.md` makes for its typed `jsonb` rule graph: this plan is the
earlier precedent for it.

Its vocabulary predates the lifecycle carve — `IContract`, `IContractAccessor`, a separate "Contract
module", TPT `ContractEntity`. Read it for the decision and its reasoning, not its symbol names.

## Still open from it

Its "Out of scope" paragraph — making the offer `ContractEntity` itself immutable/versioned — is now
owned by `DEAL_CONFIGURATION_PLAN.md` Phase 2, not by this plan.
