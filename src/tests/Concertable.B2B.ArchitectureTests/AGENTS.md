# Concertable.B2B.ArchitectureTests — architecture tests

**B2B's architecture fitness functions, of two kinds. Static: ArchUnitNET rules over B2B's compiled
assemblies, asserting a structural invariant no single test can. Dynamic (`B2BHostGraphTests`): building
each B2B host's real production registration graph without starting it, to prove the composition roots
`ValidateOnBuild` cannot. A test about one type's behaviour belongs in that module's `*.UnitTests`.**

The static rules being asserted are the `module-structure` skill: cross-module isolation once a type is
`public`, and the layer reference graph. A rule's `.Because(...)` string is what a failing build shows the
developer, so it names the doc that states the rule. The dynamic host-graph coverage and activation rules
are the `composition-testing` skill.
