# Application module — Technical Debt

## LOW

### Concert availability projection is modelled as a public Domain entity

`ConcertAvailabilityEntity` is an Application-owned persistence projection populated from Concert lifecycle events, but it is exposed from the Domain layer and named as though Concert owns it. The cross-module data flow is correct; the type placement and vocabulary are not.

**Resolves when:** the projection becomes an internal Application Infrastructure read model with Application-owned availability-reservation vocabulary throughout its entity, configuration, context surface, handler, seed surface, tests, and initial migration.

## MEDIUM

### Accept and Withdraw/Reject/Cancel conflict classification has no unit coverage

`ApplicationWorkflow` and `ApplicationService` decide which `DbUpdateException` each transition treats as
expected, and those predicates are proven only by the integration race tests. Reverting a predicate to a
narrower or broader one leaves the unit suites green, and `Concertable.B2B.Application.UnitTests` has no
`Moq` reference to drive the workflow's dependencies. The blocker is that a `DbUpdateException` carrying
populated `Entries` — which every row-scoped predicate reads — needs a live provider to construct, and
`IsApplicationAcceptanceConflict`'s duplicate-key arm needs real SQL Server because `SqlException` is sealed.

**Resolves when:** the row-scoped predicates are exercised directly, either through a provider-backed test
double that can raise a populated `DbUpdateException` or by a fixture-level harness that forces each
classified failure and asserts the reported error.
