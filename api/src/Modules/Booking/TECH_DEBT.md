# Booking tech debt

## Contract immutability is proven structurally, never through a live deal edit

`ContractApiTests.Get_ReturnsImmutable*Snapshot` assert that acceptance closes the deal to further edits: the
opportunity is booked and a venue's editable set is its open opportunities, so the terms behind a signed
contract cannot drift, and the other drift path — editing while the application is still pending — is closed
by the terms fingerprint the acceptance checks. Both halves hold, but no test edits a deal and re-reads the
contract, so nothing would catch a snapshot that started reading through to its opportunity.

The path that would exercise it is available: cancel the booking, which reopens the opportunity, then edit the
deal and find the contract unchanged. It was not written because the reopen arrives through cancellation,
escrow refund and the opportunity-reopen handler, making it the most timing-sensitive assertion in the suite.

**Resolves when:** one deal type exercises edit-after-reopen against a persisted contract, waiting on the
opportunity's reopen rather than on a delay.

## The agreed economics are stored three times and enumerated twice more

`FlatFeeDealEntity.Fee`, `DoorSplitDealEntity.ArtistDoorPercent`, `VersusDealEntity.Guarantee` and
`VenueHireDealEntity.HireFee` are copied onto the contract's frozen terms, then again onto
`ConcertEntity.{Fee, HireFee, ArtistDoorPercent, Guarantee}` as
four nullables filled by a type switch and read back by a `DealType` switch on `!.Value`. A copy is required —
every deal type has `Update(...)`, so settlement must not read the live deal — but `ContractEntity`, the
entity that already holds the signatures, `TermsText` and the PDF, is the one place holding no figures.

Two more sites enumerate the same per-arm fields: `ApplicationTermsFingerprint.Calculate` and the `IDealTerms`
renderers. Neither is wrong in shape, but nothing forces a new field on a deal arm through either, so the
fingerprint can silently stop covering a term it is supposed to bind.

**Resolves when:** `ConcertEntity` carries one arm per deal type in place of its four nullables, the way
`ContractEntity` already does; and the fingerprint's per-arm payload is dispatched rather than switched — as
its own keyed family, not a second member beside `IDealTerms.Render`, so a wording edit cannot sit next to a
hash input.
