# Concertable.B2B.ArchitectureTests — architecture tests

**Its own tier: an ArchUnitNET rule over B2B's compiled assemblies, asserting a structural invariant no
single test can. A test about one type's behaviour belongs in that module's `*.UnitTests`.**

The rules being asserted are the `module-structure` skill and the
`module-structure` skill: cross-module isolation once a type is `public`, and the layer reference graph.
A rule's `.Because(...)` string is what a failing build shows the developer, so it names the doc that
states the rule.