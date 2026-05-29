## Summary

<!-- What changed and why. Lead with the business outcome, not the code mechanics. -->

## Affected Module(s)

<!-- Tick every bounded context this PR touches. Cross-module PRs need careful review (see Architecture Compliance). -->

- [ ] Meetings
- [ ] Administration
- [ ] Payments
- [ ] Registrations
- [ ] UserAccess
- [ ] BuildingBlocks (cross-cutting)
- [ ] API host / composition
- [ ] Database scripts

## Type of Change

- [ ] Command / Write model (aggregate, command handler, validator)
- [ ] Query / Read model (Dapper handler, SQL view)
- [ ] Domain event (intra-module side effect)
- [ ] Integration event (cross-module contract — **breaking changes need coordination**)
- [ ] Database schema (new `Database/<schema>/NNNN_*.sql` script — never modify deployed scripts)
- [ ] Infrastructure / Autofac composition root
- [ ] Background job (Quartz)
- [ ] Bug fix
- [ ] Refactor (no behaviour change)
- [ ] Docs / ADR

## Architecture Compliance

<!-- These are the rules from CLAUDE.md. If any are unchecked, justify in the PR body. -->

- [ ] No direct cross-module method calls — module-to-module is integration events only
- [ ] Domain layer has no infrastructure dependencies (pure POCO)
- [ ] Business logic lives in the aggregate, not the command handler
- [ ] Invariants enforced via `CheckRule(IBusinessRule)`
- [ ] Validators use FluentValidation in the Application layer
- [ ] Query handlers use Dapper + `[schema].[v_*]` views, return DTOs only
- [ ] Module arch tests pass (`dotnet test src/Modules/{Module}/Tests/ArchTests`)
- [ ] Cross-module arch tests pass (`dotnet test src/Tests/ArchTests`)
- [ ] If a non-trivial decision was made: ADR added under `docs/architecture-decision-log/`

## Tests

- [ ] Unit tests (domain — aggregates, value objects, business rules)
- [ ] Integration tests (handler + real SQL Server)
- [ ] System integration tests (if cross-module flow changed)
- [ ] Manual verification via Swagger at `http://localhost:5000/swagger`

## Database Changes

<!-- Delete this section if no DB changes. -->

- Schema: `<module schema>`
- New scripts: `<list of NNNN_*.sql files>`
- [ ] Scripts are additive only (no edits to previously-deployed scripts)
- [ ] Read-model views updated if write model shape changed

## Related

- ADR(s): <link or N/A>
- Spec: <`docs/spec-driven/...` path or N/A>
- Issue: <link or N/A>

---

<sub>PR scaffolded against this repo's modular monolith / DDD conventions. See `CLAUDE.md` and `docs/architecture-decision-log/` for the rules behind each checkbox.</sub>
