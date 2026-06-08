# Venue app — pending features

- **Venue location editing** — not implemented anywhere in the UI. `MyVenuePage` shows location
  read-only; `CreateVenuePage` submits hardcoded default coordinates (London). When built, add
  `onLocationChange` to `VenueEdits` in `web/shared/src/features/venues/venueSections.tsx` and an
  edit mode to `LocationSection`.
- **Venue concerts section** — `VenueConcerts` is a static "No upcoming concerts." placeholder; no
  data behind it yet.
