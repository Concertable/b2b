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
