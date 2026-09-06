# Dashboard tech debt

## Calculate dashboard KPI deltas

The artist and venue KPI contracts retain nullable comparison fields, but their services currently
return `null` because only current-period totals are available. Define the comparison period and
zero-baseline behaviour, add the required historical application and payment-reporting queries, populate
the artist payout plus venue application and revenue deltas, and cover the resulting wire values.

## Remove the transitive Opportunity query from Application dashboard counts

Venue KPI composition is serialized because the Application pending-count query calls back through
`IOpportunityModule` and can otherwise overlap the dashboard's direct Opportunity count on the same
scoped read context. Replace that reporting-only dependency with the selective, event-fed
Application-owned availability projection in
[`plans/launch/LIFECYCLE_READ_PROJECTIONS_PLAN.md`](../../../../../plans/launch/LIFECYCLE_READ_PROJECTIONS_PLAN.md).

**Resolves when:** pending-application counts use only Application persistence, authoritative command
paths still query Opportunity directly, projection delivery and recovery are covered, and the dashboard
can safely parallelize only dependency-disjoint reads.
