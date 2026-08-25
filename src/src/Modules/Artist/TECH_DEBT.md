# Artist tech debt

## `Genres` allows duplicate tags

`ArtistEntity.Genres` is `List<Genre>`, so the same genre can be added twice — a set is the correct shape for a tag collection. Mapped via EF Core's `PrimitiveCollection` (a JSON column), which has a known query-time bug with `ICollection<T>` (dotnet/efcore#35502) and no confirmed support for `HashSet<T>`; switching the backing type needs that verified against this EF Core version before landing, not assumed.

Owner decision: verify `PrimitiveCollection` + `HashSet<Genre>` querying (`.Contains`, filtering) works correctly on the pinned EF Core version, then change the property and re-scaffold the Artist migration.

Resolves when: `Genres` is a set-shaped type and a test proves a duplicate genre cannot be added.
