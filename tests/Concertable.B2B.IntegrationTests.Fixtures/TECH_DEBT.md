# Concertable.B2B.IntegrationTests.Fixtures — technical debt

## LOW

### `ApiFixture.CreateClient` takes a full `UserEntity` instead of a minimal identity

`CreateClient(UserEntity user)` only ever reads `user.Id`/`user.Email` to set `TestAuthHandler`'s
headers — it never persists or queries the entity. Most call sites pass a real, already-seeded
`UserEntity` (e.g. `fixture.SeedState.ArtistManager1`), which is fine. But
`AdminProvisioningTests.LogInAsync(Guid userId, string email)` calls
`UserEntity.FromRegistration(userId, email)` purely to satisfy this signature, for a user that was
already persisted moments earlier by `RegisterAsync` — reusing the production domain factory
(`Concertable.B2B.User.Domain.Entities.UserEntity.FromRegistration`, the one
`CredentialRegisteredHandler` uses to construct a brand-new entity from a registration event) to fake an
identity for a row that already exists, instead of reading the real one back.

The `email` parameter on `LogInAsync` is dead weight: `userId` is enough to look up the real persisted
email once `RegisterAsync` has run. Passing a second, separately-tracked `email` value invites drift — a
test could pass a `userId`/`email` pair that don't actually belong to the same row, silently
authenticating as claims that don't match the real DB state, rather than failing loudly.

**Resolves when:** `LogInAsync(Guid userId)` alone, looking up the persisted `UserEntity.Email` for
`userId` via `UserDbContext` (or a small fixture helper) instead of accepting a caller-supplied copy.
More broadly, `CreateClient` could accept a minimal test-identity shape (`Guid id, string email`, or a
dedicated record) rather than a full domain `UserEntity`, so callers stop needing to construct or borrow
one just to authenticate. Bundle with the `UserEntity.Create` rename in
`api/Concertable.B2B/src/Modules/User/TECH_DEBT.md`, since fixing this changes the same call sites.

Found 2026-08-20 while debugging the `CredentialRegisteredHandler` `SaveChangesAsync` regression (once
the user was actually persisted, the pattern became worth naming). Not fixed here — logged for a
follow-up pass.
