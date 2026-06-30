# Concertable.B2B

The **B2B** service of [Concertable](https://github.com/Concertable/concertable) — the
business-facing side of the marketplace, where venue and artist managers register, manage their
venues/artists, and create concerts. It is a *data service*: it owns its data and talks to other
data services (Customer, Search) only through `*.Contracts` integration events, never their runtime.
It depends on the **Auth** and **Payment** adapter services at runtime.

## Canonical source vs. this mirror

Development happens in the **monorepo** ([`Concertable/concertable`](https://github.com/Concertable/concertable)),
under `api/Concertable.B2B/`. That folder is **automatically mirrored** to the read-only repo
[`Concertable/concertable-b2b`](https://github.com/Concertable/concertable-b2b) on every push to
`master`. **Don't open PRs against the mirror** — nothing flows back from it.

## Building standalone

The deployable closure consumes Concertable's shared platform and cross-service contracts as NuGet
`PackageReference`s from the private org feed `https://nuget.pkg.github.com/Concertable`. Restoring
them needs a GitHub [personal access token](https://github.com/settings/tokens) with the
**`read:packages`** scope, exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build Concertable.B2B.Web/Concertable.B2B.Web.csproj
dotnet build Concertable.B2B.Workers/Concertable.B2B.Workers.csproj
```

Building the two host projects pulls the whole deployable closure. (In the monorepo's CI the same
variable is supplied by the workflow's `GITHUB_TOKEN`; standalone, you export your own PAT.)
