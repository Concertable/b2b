# scripts — release-candidate verification

**Is what CI just built fit to be promoted?** These four run after the build in `ci.yml` and answer that
without publishing anything: migration snapshots match the model, the packed IDs are exactly the promotion
manifest's and a clean consumer can bind them, the built archives carry the tag they claim, and every
candidate is inventoried with an SBOM and a scan. Nothing here pushes, tags or releases — `10C` does that,
from artifacts these have already cleared.

All but `verify-package-candidates.ps1`, which is handed a directory, resolve the repository root from
`$PSScriptRoot/..` and work in paths relative to it. `verify-artifact-integrity.ps1` then mounts that root
into each scanner container at `/workspace`, so every path it passes must stay repository-relative — an
absolute host path works when you run it locally and means nothing inside the container.

## Both container archive layouts are in play, and only one of them is a tar a scanner can read

`Concertable.B2B.Workers` is an Azure Functions host and its base image is OCI-format, so
`PublishContainer` writes an **OCI image layout** — `oci-layout` plus an `index.json` image index, no
`manifest.json`, and not gzipped despite the `.tar.gz` name. Web and the seeding simulator write a Docker
archive. Trivy reads an OCI layout only as a *directory*, so `verify-artifact-integrity.ps1` unpacks that
one and hands over the directory.

The condition for unpacking is the **absence of `manifest.json`**, never the presence of `oci-layout`:
`docker save` writes a hybrid carrying both, and keying on `oci-layout` drags the two Docker archives onto
a path they do not need. Read the entries of a real archive before changing this — the layouts are not
distinguishable by file extension.

## `@($null)` has one element, and the empty case is the one CI never shows you

A Trivy result set with no vulnerabilities deserialises to `$null`, so `@($result.Vulnerabilities)` is a
one-element array holding nothing. A gate that iterates it reports one blank finding per result set and
fails every *clean* image — and no red image reproduces it, so CI cannot catch it. Filter with
`Where-Object { $null -ne $_ }` before iterating anything deserialised from a scanner, and exercise a new
gate against a report with findings **and** one without before trusting it.

## A suppression is a decision, so keep it where a reader will find it

`verify-artifact-integrity.ps1` scans vulnerabilities at HIGH and CRITICAL, and secrets at every
severity, both with `--ignorefile /dev/null` and `--exit-code 0`, then decides what blocks in
`Get-BlockingVulnerabilities`. That is deliberate: narrowing what the scanner *collects* hides the
finding from the retained evidence too, while narrowing what the gate *blocks* leaves the report intact
and the reasoning readable. Anything tolerated belongs in
`$toleratedUnfixedPackages` with a comment, and in `TECH_DEBT.md` with a condition that lets it be deleted.
