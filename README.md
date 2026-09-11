# Concertable.B2B

The **B2B** service of [Concertable](https://github.com/Concertable/concertable) — the
business-facing side of the marketplace, where venue and artist managers register, manage their
venues/artists, and create concerts. It is a *data service*: it owns its data and talks to other
data services (Customer, Search) only through `*.Contracts` integration events, never their runtime.
It depends on the **Auth** and **Payment** adapter services at runtime.

## Canonical source

This repository is the canonical source for the B2B service. B2B changes are developed and
reviewed here; shared platform and cross-service contract changes are delivered through their
owning repositories and consumed as packages.

## Building standalone

The deployable closure consumes Concertable's shared platform and cross-service contracts as NuGet
`PackageReference`s from the private org feed `https://nuget.pkg.github.com/Concertable`. Restoring
them needs a GitHub [personal access token](https://github.com/settings/tokens) with the
**`read:packages`** scope, exported as `GITHUB_PACKAGES_TOKEN` (the `nuget.config` reads it):

```sh
export GITHUB_PACKAGES_TOKEN=<your read:packages PAT>
dotnet build Concertable.B2B.slnx
```

Building the solution pulls the whole deployable closure. (In the repository's CI the same
variable is supplied by the workflow's `GITHUB_TOKEN`; standalone, you export your own PAT.)
