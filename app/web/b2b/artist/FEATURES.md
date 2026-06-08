# Artist app — pending features

- **Artist location editing** — not implemented anywhere in the UI. `MyArtistPage` shows location
  read-only; `CreateArtistPage` submits hardcoded default coordinates (London). When built, add
  `onLocationChange` to `ArtistEdits` in `web/shared/src/features/artists/artistSections.tsx` and an
  edit mode to `LocationSection`.
- **Artist concerts section** — `ArtistConcerts` is a static "No upcoming concerts." placeholder; no
  data behind it yet.
- **Genre editing on create** — `CreateArtistPage` submits `draft.genres` but the create canvas has
  no genre picker section.
