# Application module — Technical Debt

## LOW

### Concert availability projection is modelled as a public Domain entity

`ConcertAvailabilityEntity` is an Application-owned persistence projection populated from Concert lifecycle events, but it is exposed from the Domain layer and named as though Concert owns it. The cross-module data flow is correct; the type placement and vocabulary are not.

**Resolves when:** the projection becomes an internal Application Infrastructure read model with Application-owned availability-reservation vocabulary throughout its entity, configuration, context surface, handler, seed surface, tests, and initial migration.
