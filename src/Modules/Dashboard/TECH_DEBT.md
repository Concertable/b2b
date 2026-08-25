# Dashboard tech debt

## Calculate dashboard KPI deltas

The artist and venue KPI contracts retain nullable comparison fields, but their services currently
return `null` because only current-period totals are available. Define the comparison period and
zero-baseline behaviour, add the required historical application and payment-reporting queries, populate
the artist payout plus venue application and revenue deltas, and cover the resulting wire values.
