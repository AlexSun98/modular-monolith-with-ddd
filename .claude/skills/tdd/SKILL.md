---
name: tdd
description: Test-driven development with red-green-refactor loop. Use when user wants to build features or fix bugs using TDD, mentions "red-green-refactor", wants integration tests, or asks for test-first development.
---

# Test-Driven Development

## Philosophy

**Core principle**: Tests should verify behavior through public interfaces, not implementation details. Code can change entirely; tests shouldn't.

**Good tests** exercise real code paths through public APIs and read like specifications. They survive refactors because they don't care about internal structure.

**Bad tests** are coupled to implementation — they mock internal collaborators, test private methods, or assert on internal state. Warning sign: your test breaks when you refactor, but behavior hasn't changed.

## In this project

Test hierarchy:
- **Unit tests** — Domain model only (Aggregates, Entities, Value Objects). No infrastructure. Test via the aggregate's public methods. `CheckRule` violations throw `BusinessRuleValidationException`.
- **Integration tests** — Full module stack with real SQL Server. Mock only external deps (email, payment gateways). Call `{Module}.ExecuteCommandAsync` / `ExecuteQueryAsync`.
- **Architecture tests** — NetArchTest rules enforcing layer dependency constraints.

Framework: **NUnit** + **NSubstitute**. See `docs/copilot-instructions/03-TESTING-GUIDELINES.md` for full conventions.

Use domain terms from `docs/catalog-of-terms/` for test names and method vocabulary. Check `docs/architecture-decision-log/` for ADRs in the area you're touching before writing tests.

## Anti-Pattern: Horizontal Slices

**DO NOT write all tests first, then all implementation.** This produces tests that verify *imagined* behavior rather than *actual* behavior.

**Correct approach**: Vertical slices via tracer bullets.

```
WRONG (horizontal):
  RED:   test1, test2, test3, test4, test5
  GREEN: impl1, impl2, impl3, impl4, impl5

RIGHT (vertical):
  RED→GREEN: test1→impl1
  RED→GREEN: test2→impl2
  RED→GREEN: test3→impl3
```

## Workflow

### 1. Planning

Before writing any code:

- [ ] Confirm with user what interface changes are needed
- [ ] Confirm which behaviors to test (prioritize critical paths and invariants)
- [ ] For commands: identify the aggregate method, invariants it enforces, domain events it raises
- [ ] For queries: identify what SQL view it queries — integration test is the right level
- [ ] List behaviors to test (not implementation steps)
- [ ] Get user approval on the plan

**You can't test everything.** Focus on critical paths, business rules, and complex logic.

### 2. Tracer Bullet

Write ONE test that confirms ONE thing:

```
RED:   Write test for first behavior → fails
GREEN: Write minimal code to pass → passes
```

Example for a domain unit test:
```csharp
[Test]
public void AddingMember_WhenGroupIsFull_BreaksMaxMembersRule()
{
    var group = MeetingGroupTestFactory.CreateWithMaxMembers(5);

    Assert.That(
        () => group.AddMember(MemberId.Of(Guid.NewGuid())),
        Throws.TypeOf<BusinessRuleValidationException>());
}
```

### 3. Incremental Loop

For each remaining behavior:

```
RED:   Write next test → fails
GREEN: Minimal code to pass → passes
```

Rules:
- One test at a time
- Only enough code to pass current test
- Don't anticipate future tests

### 4. Refactor

After all tests pass:

- [ ] Extract duplication (NUnit `[SetUp]` or test factory helpers)
- [ ] Deepen aggregates — move complexity behind simple public methods
- [ ] Run `dotnet test` after each refactor step

**Never refactor while RED.** Get to GREEN first.

## Checklist Per Cycle

```
[ ] Test describes behavior, not implementation
[ ] Test uses public interface only
[ ] Test would survive internal refactor
[ ] Code is minimal for this test
[ ] No speculative features added
[ ] Domain terms match docs/catalog-of-terms/
```
