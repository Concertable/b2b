# Concertable.B2B.StartupTests — startup tests

**Would any B2B host refuse to boot, given the configuration its app model supplies it?** These build the
real production registration graphs (Web, Workers, Seed Simulator) and the AppHost's resource graph without
starting any of them or reaching external infrastructure. Tests that execute requests, business operations
or infrastructure belong in integration or E2E projects; tests that assert code structure belong in
`Concertable.B2B.ArchitectureTests`.

Host coverage and activation rules: the `composition-testing` skill.
