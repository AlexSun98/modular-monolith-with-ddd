# .NET 10.0 Upgrade Plan

## Table of Contents

- [Executive Summary](#executive-summary)
- [Migration Strategy](#migration-strategy)
- [Detailed Dependency Analysis](#detailed-dependency-analysis)
- [Project-by-Project Plans](#project-by-project-plans)
- [Package Update Reference](#package-update-reference)
- [Breaking Changes Catalog](#breaking-changes-catalog)
- [Testing Strategy](#testing-strategy)
- [Risk Management](#risk-management)
- [Complexity & Effort Assessment](#complexity--effort-assessment)
- [Source Control Strategy](#source-control-strategy)
- [Success Criteria](#success-criteria)

---

## Executive Summary

### Scenario Description

Upgrade the CompanyName.MyMeetings Modular Monolith solution from **.NET 8.0** to **.NET 10.0 (LTS)**.

### Scope

| Metric | Value |
|--------|-------|
| **Total Projects** | 48 |
| **Projects Requiring Upgrade** | 47 (1 stays on netstandard2.1) |
| **Total NuGet Packages** | 27 |
| **Packages Requiring Update** | 5 |
| **Total Code Files** | 1,062 |
| **Files with Issues** | 72 |
| **Total Lines of Code** | 38,320 |
| **Estimated LOC to Modify** | ~77 (~0.2% of codebase) |

### Current State

- All projects target `net8.0` (except `Database.Build` which targets `netstandard2.1`)
- Solution uses Modular Monolith architecture with DDD patterns
- 5 business modules: Administration, Meetings, Payments, Registrations, UserAccess
- Shared BuildingBlocks layer for cross-cutting concerns
- Comprehensive test suite (Unit, Integration, Architecture tests)

### Target State

- All applicable projects target `net10.0`
- Entity Framework Core upgraded to 10.0.1
- All source-incompatible APIs updated
- Solution builds and all tests pass

### Selected Strategy

**All-At-Once Strategy** — All projects upgraded simultaneously in a single atomic operation.

**Rationale:**
- 48 projects (medium-sized solution, manageable scope)
- All currently on .NET 8.0 (homogeneous codebase)
- Clear dependency structure (8 levels, no circular dependencies)
- All packages have target framework versions available
- No security vulnerabilities to address
- Low-risk changes (source incompatibilities, not binary breaking)

### Complexity Assessment

| Dimension | Assessment |
|-----------|------------|
| **Solution Size** | Medium (48 projects, 38K LOC) |
| **Dependency Depth** | 8 levels (moderate) |
| **Issue Severity** | Low (0 binary breaking, 72 source incompatible) |
| **Package Risk** | Low (all have known upgrades) |
| **Overall Complexity** | 🟢 Low |

### Critical Issues

| Category | Count | Impact |
|----------|-------|--------|
| Target Framework Changes | 47 | All projects need `net8.0` → `net10.0` |
| Source Incompatible APIs | 72 | `TimeSpan.FromSeconds/Milliseconds`, `SqlConnection` |
| Behavioral Changes | 5 | `Environment.SetEnvironmentVariable` in build project |
| Incompatible Packages | 1 | `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` |
| Deprecated Packages | 1 | `SqlStreamStore.MsSql` (optional to address) |

---

## Migration Strategy

### Approach: All-At-Once (Atomic Upgrade)

All 47 projects will be upgraded simultaneously in a single coordinated operation. This approach is selected because:

1. **Homogeneous Codebase**: All projects currently target .NET 8.0
2. **Clear Dependencies**: No circular dependencies, well-structured 8-level hierarchy
3. **Low Risk Profile**: No binary breaking changes, only source incompatibilities
4. **Package Compatibility**: All packages have known .NET 10 compatible versions

### Execution Phases

| Phase | Description | Deliverable |
|-------|-------------|-------------|
| **Phase 0: Preparation** | Verify .NET 10 SDK installed, update global.json if present | Environment ready |
| **Phase 1: Atomic Upgrade** | Update all project files, packages, and fix compilation errors | Solution builds with 0 errors |
| **Phase 2: Test Validation** | Execute all test projects, address failures | All tests pass |

### Key Principles

- **Single Atomic Operation**: All framework and package updates happen together
- **No Intermediate States**: Solution moves directly from .NET 8 to .NET 10
- **Build-Fix-Verify Cycle**: Build, fix all errors, rebuild until clean
- **Test After Build**: Run tests only after successful compilation

---

## Detailed Dependency Analysis

### Dependency Graph Summary

The solution has **8 dependency levels** with clear bottom-up structure:

```
Level 0 (Foundation)     → BuildingBlocks.Domain, BuildingBlocks.IntegrationTests, _build, Database projects
Level 1 (Domain)         → Module Domain projects (Administration, Meetings, Payments, Registrations, UserAccess)
Level 2 (Infrastructure) → BuildingBlocks.Infrastructure, BuildingBlocks.Application
Level 3 (Events)         → Module IntegrationEvents projects
Level 4 (Application)    → Module Application projects
Level 5 (Module Infra)   → Module Infrastructure projects
Level 6 (Tests)          → Module Test projects (Unit, Arch, Integration)
Level 7 (API)            → CompanyName.MyMeetings.API, Registrations tests
Level 8 (Top-Level)      → Solution-level tests (ArchTests, IntegrationTests, SUT)
```

### Project Groupings

#### Foundation Projects (Level 0) - 6 projects
| Project | Dependencies | Dependants |
|---------|--------------|------------|
| `BuildingBlocks.Domain` | None | 6 domain projects |
| `BuildingBlocks.IntegrationTests` | None | 18 test projects |
| `_build` | None | None (standalone) |
| `Database.Build` | None | None (netstandard2.1, no upgrade needed) |
| `Database.sqlproj` | None | None |
| `DatabaseMigrator` | None | None |

#### Domain Projects (Level 1) - 6 projects
| Project | Dependencies |
|---------|--------------|
| `Modules.Administration.Domain` | BuildingBlocks.Domain |
| `Modules.Meetings.Domain` | BuildingBlocks.Domain |
| `Modules.Payments.Domain` | BuildingBlocks.Domain |
| `Modules.Registrations.Domain` | BuildingBlocks.Domain |
| `Modules.UserAccess.Domain` | BuildingBlocks.Domain |
| `BuildingBlocks.Application` | BuildingBlocks.Domain |

#### Shared Infrastructure (Level 2) - 2 projects
| Project | Dependencies |
|---------|--------------|
| `BuildingBlocks.Infrastructure` | BuildingBlocks.Application |
| `BuildingBlocks.Application.UnitTests` | BuildingBlocks.Application |

#### Integration Events (Level 3) - 5 projects
| Project | Dependencies |
|---------|--------------|
| `Modules.Administration.IntegrationEvents` | BuildingBlocks.Infrastructure |
| `Modules.Meetings.IntegrationEvents` | BuildingBlocks.Infrastructure |
| `Modules.Payments.IntegrationEvents` | BuildingBlocks.Infrastructure |
| `Modules.Registrations.IntegrationEvents` | BuildingBlocks.Infrastructure |
| `Modules.UserAccess.IntegrationEvents` | BuildingBlocks.Infrastructure |

#### Application Projects (Level 4) - 5 projects
| Project | Dependencies |
|---------|--------------|
| `Modules.Administration.Application` | IntegrationEvents, Domain |
| `Modules.Meetings.Application` | IntegrationEvents, Domain |
| `Modules.Payments.Application` | IntegrationEvents, Domain |
| `Modules.Registrations.Application` | IntegrationEvents, Domain, BuildingBlocks.Application |
| `Modules.UserAccess.Application` | IntegrationEvents, Domain |

#### Module Infrastructure (Level 5) - 4 projects
| Project | Dependencies |
|---------|--------------|
| `Modules.Administration.Infrastructure` | Modules.Administration.Application |
| `Modules.Meetings.Infrastructure` | Modules.Meetings.Application |
| `Modules.Payments.Infrastructure` | Modules.Payments.Application |
| `Modules.UserAccess.Infrastructure` | Modules.UserAccess.Application |

#### Module Tests (Level 6) - 14 projects
- 5 ArchTests projects (one per module)
- 5 UnitTests projects (one per module)  
- 4 IntegrationTests projects (Administration, Meetings, Payments, UserAccess)
- Plus: `Modules.Registrations.Infrastructure`

#### API & Registrations Tests (Level 7) - 4 projects
| Project | Dependencies |
|---------|--------------|
| `CompanyName.MyMeetings.API` | All Module Infrastructure projects |
| `Modules.Registrations.ArchTests` | Registrations.Infrastructure |
| `Modules.Registrations.Domain.UnitTests` | Registrations.Infrastructure |
| `Modules.Registrations.IntegrationTests` | Registrations.Infrastructure |

#### Top-Level Tests (Level 8) - 3 projects
| Project | Dependencies |
|---------|--------------|
| `CompanyName.MyMeetings.ArchTests` | API |
| `CompanyName.MyMeetings.IntegrationTests` | API |
| `CompanyName.MyMeetings.SUT` | API |

### Critical Path

The critical path for this upgrade is:

```
BuildingBlocks.Domain → BuildingBlocks.Application → BuildingBlocks.Infrastructure 
    → IntegrationEvents → Module Applications → Module Infrastructures → API → Tests
```

All projects must be updated atomically since they share common dependencies.

---

## Project-by-Project Plans

### All Projects - Atomic Update

Since we're using the **All-At-Once Strategy**, all projects are updated simultaneously. Below are the specific changes required for each project category.

#### Projects Requiring Target Framework Change Only (39 projects)

These projects only need `<TargetFramework>net8.0</TargetFramework>` → `<TargetFramework>net10.0</TargetFramework>`:

**BuildingBlocks:**
- `BuildingBlocks\Domain\CompanyName.MyMeetings.BuildingBlocks.Domain.csproj`
- `BuildingBlocks\Tests\Application.UnitTests\CompanyName.MyMeetings.BuildingBlocks.Application.UnitTests.csproj`
- `BuildingBlocks\Tests\IntegrationTests\CompanyName.MyMeetings.BuildingBlocks.IntegrationTests.csproj`

**Module Domains:**
- `Modules\Administration\Domain\CompanyName.MyMeetings.Modules.Administration.Domain.csproj`
- `Modules\Meetings\Domain\CompanyName.MyMeetings.Modules.Meetings.Domain.csproj`
- `Modules\Payments\Domain\CompanyName.MyMeetings.Modules.Payments.Domain.csproj`
- `Modules\Registrations\Domain\CompanyName.MyMeetings.Modules.Registrations.Domain.csproj`
- `Modules\UserAccess\Domain\CompanyName.MyMeetings.Modules.UserAccess.Domain.csproj`

**Module Applications:**
- `Modules\Administration\Application\CompanyName.MyMeetings.Modules.Administration.Application.csproj`
- `Modules\Meetings\Application\CompanyName.MyMeetings.Modules.Meetings.Application.csproj`
- `Modules\Registrations\Application\CompanyName.MyMeetings.Modules.Registrations.Application.csproj`
- `Modules\UserAccess\Application\CompanyName.MyMeetings.Modules.UserAccess.Application.csproj`

**Module IntegrationEvents:**
- `Modules\Administration\IntegrationEvents\CompanyName.MyMeetings.Modules.Administration.IntegrationEvents.csproj`
- `Modules\Meetings\IntegrationEvents\CompanyName.MyMeetings.Modules.Meetings.IntegrationEvents.csproj`
- `Modules\Payments\IntegrationEvents\CompanyName.MyMeetings.Modules.Payments.IntegrationEvents.csproj`
- `Modules\Registrations\IntegrationEvents\CompanyName.MyMeetings.Modules.Registrations.IntegrationEvents.csproj`
- `Modules\UserAccess\IntegrationEvents\CompanyName.MyMeetings.Modules.UserAccess.IntegrationEvents.csproj`

**Module Tests (Unit, Arch):**
- `Modules\Administration\Tests\ArchTests\CompanyName.MyMeetings.Modules.Administration.ArchTests.csproj`
- `Modules\Administration\Tests\UnitTests\CompanyName.MyMeetings.Modules.Administration.Domain.UnitTests.csproj`
- `Modules\Meetings\Tests\ArchTests\CompanyName.MyMeetings.Modules.Meetings.ArchTests.csproj`
- `Modules\Meetings\Tests\UnitTests\CompanyName.MyMeetings.Modules.Meetings.Domain.UnitTests.csproj`
- `Modules\Payments\Tests\ArchTests\CompanyName.MyMeetings.Modules.Payments.ArchTests.csproj`
- `Modules\Payments\Tests\UnitTests\CompanyName.MyMeetings.Modules.Payments.Domain.UnitTests.csproj`
- `Modules\Registrations\Tests\ArchTests\CompanyName.MyMeetings.Modules.Registrations.ArchTests.csproj`
- `Modules\Registrations\Tests\UnitTests\CompanyName.MyMeetings.Modules.Registrations.Domain.UnitTests.csproj`
- `Modules\UserAccess\Tests\ArchTests\CompanyName.MyMeetings.Modules.UserAccess.ArchTests.csproj`
- `Modules\UserAccess\Tests\UnitTests\CompanyName.MyMeetings.Modules.UserAccess.Domain.UnitTests.csproj`

**Solution-Level Tests:**
- `Tests\ArchTests\CompanyName.MyMeetings.ArchTests.csproj`

**Database:**
- `Database\DatabaseMigrator\DatabaseMigrator.csproj`
- `Database\CompanyName.MyMeetings.Database\CompanyName.MyMeetings.Database.sqlproj`

#### Projects Requiring Framework + Package Updates (2 projects)

**BuildingBlocks.Application** (`BuildingBlocks\Application\CompanyName.MyMeetings.BuildingBlocks.Application.csproj`):
- Target Framework: `net8.0` → `net10.0`
- Package: `Newtonsoft.Json` 13.0.3 → 13.0.4

**BuildingBlocks.Infrastructure** (`BuildingBlocks\Infrastructure\CompanyName.MyMeetings.BuildingBlocks.Infrastructure.csproj`):
- Target Framework: `net8.0` → `net10.0`
- Package: `Microsoft.EntityFrameworkCore` 8.0.0 → 10.0.1
- Package: `Microsoft.EntityFrameworkCore.SqlServer` 8.0.0 → 10.0.1
- ⚠️ Deprecated: `SqlStreamStore.MsSql` 1.1.3 (keep as-is, no replacement available)

#### Projects Requiring Framework + Code Changes (8 projects)

These projects have source-incompatible API usages that need fixing:

**Module Infrastructures** (5 projects with `TimeSpan.FromSeconds` changes):
- `Modules\Administration\Infrastructure\CompanyName.MyMeetings.Modules.Administration.Infrastructure.csproj`
- `Modules\Meetings\Infrastructure\CompanyName.MyMeetings.Modules.Meetings.Infrastructure.csproj`
- `Modules\Payments\Infrastructure\CompanyName.MyMeetings.Modules.Payments.Infrastructure.csproj`
- `Modules\Registrations\Infrastructure\CompanyName.MyMeetings.Modules.Registrations.Infrastructure.csproj`
- `Modules\UserAccess\Infrastructure\CompanyName.MyMeetings.Modules.UserAccess.Infrastructure.csproj`

**Payments Application** (1 project with `Rfc2898DeriveBytes` change):
- `Modules\Payments\Application\CompanyName.MyMeetings.Modules.Payments.Application.csproj`

**Build Project** (behavioral changes):
- `build\_build.csproj`

#### Projects Requiring Framework + Integration Test Code Changes (6 projects)

These test projects have `SqlConnection` usage that may need attention:
- `Modules\Administration\Tests\IntegrationTests\CompanyName.MyMeetings.Modules.Administration.IntegrationTests.csproj`
- `Modules\Meetings\Tests\IntegrationTests\CompanyName.MyMeetings.Modules.Meetings.IntegrationTests.csproj`
- `Modules\Payments\Tests\IntegrationTests\CompanyName.MyMeetings.Modules.Payments.IntegrationTests.csproj`
- `Modules\Registrations\Tests\IntegrationTests\CompanyNames.MyMeetings.Modules.Registrations.IntegrationTests.csproj`
- `Modules\UserAccess\Tests\IntegrationTests\CompanyNames.MyMeetings.Modules.UserAccess.IntegrationTests.csproj`
- `Tests\IntegrationTests\CompanyName.MyMeetings.IntegrationTests.csproj`
- `Tests\SUT\CompanyName.MyMeetings.SUT.csproj`

#### Projects Requiring Package Compatibility Resolution (1 project)

**API Project** (`API\CompanyName.MyMeetings.API\CompanyName.MyMeetings.API.csproj`):
- Target Framework: `net8.0` → `net10.0`
- ⚠️ `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` 1.19.5 is incompatible
  - **Resolution**: Update to latest compatible version or remove if Docker support not needed

#### Projects NOT Requiring Changes (1 project)

**Database.Build** (`Database\CompanyName.MyMeetings.Database.Build\CompanyName.MyMeetings.Database.Build.csproj`):
- Targets `netstandard2.1` - remains unchanged (compatible with all .NET versions)

---

## Package Update Reference

### Packages Requiring Updates

| Package | Current | Target | Projects Affected | Reason |
|---------|---------|--------|-------------------|--------|
| `Microsoft.EntityFrameworkCore` | 8.0.0 | 10.0.1 | 1 (BuildingBlocks.Infrastructure) | Framework compatibility |
| `Microsoft.EntityFrameworkCore.SqlServer` | 8.0.0 | 10.0.1 | 1 (BuildingBlocks.Infrastructure) | Framework compatibility |
| `Newtonsoft.Json` | 13.0.3 | 13.0.4 | 1 (BuildingBlocks.Application) | Recommended upgrade |

### Packages Requiring Attention

| Package | Current | Issue | Resolution |
|---------|---------|-------|------------|
| `Microsoft.VisualStudio.Azure.Containers.Tools.Targets` | 1.19.5 | Incompatible with .NET 10 | Update to latest version or remove |
| `SqlStreamStore.MsSql` | 1.1.3 | Deprecated | Keep as-is (no replacement), monitor for alternatives |

### Packages Compatible (No Changes Needed)

The following 22 packages are already compatible with .NET 10:
- Autofac 7.1.0
- Autofac.Extensions.DependencyInjection 8.0.0
- Dapper 2.1.24
- dbup-sqlserver 5.0.37
- FluentAssertions 6.12.0
- FluentValidation 11.8.1
- Hellang.Middleware.ProblemDetails 6.5.1
- IdentityServer4 4.1.2
- IdentityServer4.AccessTokenValidation 3.0.1
- MediatR 12.2.0
- Microsoft.NET.Test.Sdk 17.8.0
- NetArchTest.Rules 1.3.2
- NSubstitute 5.1.0
- Nuke.Common 7.0.6
- nunit 4.0.1
- NUnit3TestAdapter 4.5.0
- Polly 8.2.0
- Quartz 3.8.0
- Serilog.AspNetCore 8.0.0
- StyleCop.Analyzers 1.2.0-beta.556
- Swashbuckle.AspNetCore 6.5.0
- System.Data.SqlClient 4.8.6

---

## Breaking Changes Catalog

### Source Incompatible APIs (72 occurrences)

These APIs have signature changes in .NET 10 and will cause compilation errors that need fixing.

#### 1. TimeSpan Factory Methods (28 occurrences)

**Issue**: `TimeSpan.FromSeconds(double)` and `TimeSpan.FromMilliseconds(double)` now have different overload resolution in .NET 10.

**Affected Files**:
| File | Line | Code Pattern |
|------|------|--------------|
| `Modules\Administration\Infrastructure\...\ProcessInternalCommandsCommandHandler.cs` | 40 | `TimeSpan.FromSeconds(1)` |
| `Modules\Meetings\Infrastructure\...\ProcessInternalCommandsCommandHandler.cs` | 35 | `TimeSpan.FromSeconds(1)` |
| `Modules\Payments\Infrastructure\...\ProcessInternalCommandsCommandHandler.cs` | 34 | `TimeSpan.FromSeconds(1)` |
| `Modules\Registrations\Infrastructure\...\ProcessInternalCommandsCommandHandler.cs` | 35 | `TimeSpan.FromSeconds(1)` |
| `Modules\UserAccess\Infrastructure\...\ProcessInternalCommandsCommandHandler.cs` | 35 | `TimeSpan.FromSeconds(1)` |

**Resolution**: Cast integer to double explicitly:
```csharp
// Before
TimeSpan.FromSeconds(1)

// After
TimeSpan.FromSeconds(1.0)
// Or
TimeSpan.FromSeconds((double)1)
```

#### 2. SqlConnection Type (38 occurrences)

**Issue**: `System.Data.SqlClient.SqlConnection` has source incompatibility warnings in .NET 10.

**Affected Files**:
| File | Occurrences |
|------|-------------|
| `BuildingBlocks\Infrastructure\SqlConnectionFactory.cs` | 3 |
| `build\Utils\SqlReadinessChecker.cs` | 2 |
| `Tests\IntegrationTests\SeedWork\TestBase.cs` | 2 |
| `Modules\*\Tests\IntegrationTests\SeedWork\TestBase.cs` | ~20 |
| `Tests\SUT\...` | Multiple |

**Resolution**: These are typically warnings about `System.Data.SqlClient` vs `Microsoft.Data.SqlClient`. The code should still compile. If errors occur:
- Consider migrating to `Microsoft.Data.SqlClient` package (recommended for new development)
- Or suppress warnings if `System.Data.SqlClient` continues to work

#### 3. Rfc2898DeriveBytes (4 occurrences)

**Issue**: `System.Security.Cryptography.Rfc2898DeriveBytes` constructor may have different defaults.

**Affected Projects**: `Modules.Payments.Application`

**Resolution**: Explicitly specify hash algorithm parameter if not already done.

### Behavioral Changes (5 occurrences)

These changes don't cause compilation errors but may affect runtime behavior.

#### Environment.SetEnvironmentVariable

**Affected Project**: `build\_build.csproj`

**Issue**: `Environment.SetEnvironmentVariable(string, string)` behavior may differ for process-level vs machine-level variables.

**Resolution**: Review usage in build scripts. Typically no code changes needed, but test thoroughly.

---

## Testing Strategy

### Test Project Inventory

The solution contains **20 test projects** across three categories:

#### Unit Tests (6 projects)
| Project | Tests For |
|---------|-----------|
| `BuildingBlocks.Application.UnitTests` | BuildingBlocks.Application |
| `Modules.Administration.Domain.UnitTests` | Administration Domain |
| `Modules.Meetings.Domain.UnitTests` | Meetings Domain |
| `Modules.Payments.Domain.UnitTests` | Payments Domain |
| `Modules.Registrations.Domain.UnitTests` | Registrations Domain |
| `Modules.UserAccess.Domain.UnitTests` | UserAccess Domain |

#### Architecture Tests (6 projects)
| Project | Tests For |
|---------|-----------|
| `CompanyName.MyMeetings.ArchTests` | Solution-level architecture |
| `Modules.Administration.ArchTests` | Administration module architecture |
| `Modules.Meetings.ArchTests` | Meetings module architecture |
| `Modules.Payments.ArchTests` | Payments module architecture |
| `Modules.Registrations.ArchTests` | Registrations module architecture |
| `Modules.UserAccess.ArchTests` | UserAccess module architecture |

#### Integration Tests (7 projects)
| Project | Tests For |
|---------|-----------|
| `BuildingBlocks.IntegrationTests` | BuildingBlocks integration |
| `CompanyName.MyMeetings.IntegrationTests` | Solution-level integration |
| `Modules.Administration.IntegrationTests` | Administration module integration |
| `Modules.Meetings.IntegrationTests` | Meetings module integration |
| `Modules.Payments.IntegrationTests` | Payments module integration |
| `Modules.Registrations.IntegrationTests` | Registrations module integration |
| `Modules.UserAccess.IntegrationTests` | UserAccess module integration |

#### System Under Test (1 project)
| Project | Purpose |
|---------|---------|
| `CompanyName.MyMeetings.SUT` | System-level testing harness |

### Testing Approach

Since we're using the **All-At-Once Strategy**, testing happens after the complete upgrade:

1. **Build Validation First**: Ensure solution compiles with 0 errors before running tests
2. **Run All Tests Together**: Execute all test projects in a single pass
3. **Address Failures Systematically**: Fix test failures caused by framework/API changes

### Test Execution Order

```
1. Solution builds successfully (0 errors)
   ↓
2. Run Unit Tests (fastest feedback)
   - BuildingBlocks.Application.UnitTests
   - All Module Domain UnitTests (5 projects)
   ↓
3. Run Architecture Tests
   - Solution ArchTests
   - All Module ArchTests (5 projects)
   ↓
4. Run Integration Tests (requires database)
   - BuildingBlocks.IntegrationTests
   - Solution IntegrationTests
   - All Module IntegrationTests (5 projects)
   ↓
5. Run SUT Tests
   - CompanyName.MyMeetings.SUT
```

### Expected Test Issues

Based on the breaking changes identified:

| Issue | Affected Tests | Resolution |
|-------|----------------|------------|
| `SqlConnection` API changes | Integration tests | May need `Microsoft.Data.SqlClient` migration or warning suppression |
| `TimeSpan.FromSeconds` | Tests using Polly retry policies | Cast integers to doubles |
| Behavioral changes | Build project tests | Verify environment variable handling |

---

## Risk Management

### Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| **Compilation errors from API changes** | High | Medium | Well-documented breaking changes, clear resolution patterns |
| **EF Core 10 behavioral changes** | Medium | Medium | Test database operations thoroughly |
| **Incompatible container tools package** | High | Low | Update or remove package as needed |
| **SqlStreamStore deprecation** | Low | Low | Monitor for alternatives, keep current version |
| **Test failures from framework changes** | Medium | Medium | Systematic debugging, check for updated test patterns |
| **IdentityServer4 compatibility** | Low | High | Already compatible, but monitor for .NET 10 issues |

### High-Risk Changes

| Project | Risk Level | Risk Description | Mitigation |
|---------|------------|------------------|------------|
| `BuildingBlocks.Infrastructure` | Medium | EF Core upgrade + SqlConnection changes | Test all data access thoroughly |
| `API` | Medium | Container tools incompatibility | Verify Docker builds still work |
| `_build` | Low | Behavioral changes in build automation | Run build pipeline tests |

### Contingency Plans

#### If EF Core 10 Causes Issues
1. Check EF Core 10 migration guide for breaking changes
2. Review any LINQ queries for behavioral differences
3. Verify database migrations still work

#### If Container Tools Package Fails
1. Try updating to latest version: `dotnet add package Microsoft.VisualStudio.Azure.Containers.Tools.Targets`
2. If no compatible version exists, remove package and Docker support temporarily
3. Monitor for updated package release

#### If SqlStreamStore Causes Problems
1. Package is deprecated but should still work
2. Consider future migration to different event store if needed
3. No immediate action required

### Rollback Strategy

Since all changes are on branch `POC-10006-upgrade-to-NET10`:
1. **Before merge**: Simply delete branch and changes are discarded
2. **After merge**: Use `git revert` to undo merge commit
3. **Partial rollback**: Not applicable (All-At-Once doesn't support partial)

---

## Complexity & Effort Assessment

### Per-Project Complexity

| Complexity | Project Count | Criteria |
|------------|---------------|----------|
| 🟢 Low | 39 | Target framework change only |
| 🟡 Medium | 8 | Framework + code changes required |
| 🔴 High | 1 | API project with package incompatibility |

### Complexity by Category

| Category | Projects | Complexity | Notes |
|----------|----------|------------|-------|
| BuildingBlocks | 5 | Medium | Package updates + code fixes |
| Module Domains | 5 | Low | Framework only |
| Module Applications | 5 | Low-Medium | 1 has crypto API changes |
| Module IntegrationEvents | 5 | Low | Framework only |
| Module Infrastructures | 5 | Medium | TimeSpan API fixes |
| Module Tests | 18 | Low-Medium | Some have SqlConnection fixes |
| API | 1 | Medium | Package incompatibility |
| Database | 3 | Low | Framework only (1 unchanged) |
| Build | 1 | Low | Behavioral changes only |

### Resource Requirements

- **Skills Required**: .NET development, familiarity with breaking changes documentation
- **Parallel Capacity**: Single developer can complete (atomic operation)
- **Dependencies**: .NET 10 SDK must be installed

---

## Source Control Strategy

### Branching Strategy

| Branch | Purpose |
|--------|---------|
| `master` | Main branch (source) |
| `POC-10006-upgrade-to-NET10` | Upgrade branch (current) |

### Commit Strategy

For **All-At-Once Strategy**, use a single comprehensive commit:

```
Upgrade solution to .NET 10.0

- Update all 47 projects from net8.0 to net10.0
- Upgrade Entity Framework Core from 8.0.0 to 10.0.1
- Upgrade Newtonsoft.Json from 13.0.3 to 13.0.4
- Fix TimeSpan.FromSeconds API compatibility issues
- Update container tools package for .NET 10 compatibility
- All tests passing
```

### Review and Merge Process

1. **Pre-Review Checklist**:
   - [ ] Solution builds with 0 errors
   - [ ] All unit tests pass
   - [ ] All architecture tests pass
   - [ ] All integration tests pass
   - [ ] No new warnings introduced

2. **Pull Request Requirements**:
   - Title: "Upgrade to .NET 10.0"
   - Description: Summary of changes, package updates, breaking changes addressed
   - Reviewers: At least one senior developer familiar with .NET upgrades

3. **Merge Criteria**:
   - All CI checks pass
   - Code review approved
   - No merge conflicts with main branch

---

## Success Criteria

### Technical Criteria

- [ ] All 47 projects successfully target `net10.0`
- [ ] All package updates applied (EF Core 10.0.1, Newtonsoft.Json 13.0.4)
- [ ] Solution builds with 0 errors
- [ ] Solution builds with 0 new warnings (existing warnings acceptable)
- [ ] All 20 test projects pass
- [ ] No package dependency conflicts

### Quality Criteria

- [ ] Code quality maintained (no degradation in static analysis)
- [ ] Test coverage maintained (no reduction in coverage)
- [ ] Documentation updated (if applicable)

### Process Criteria

- [ ] All-At-Once strategy executed as planned
- [ ] Single commit captures all changes
- [ ] Source control strategy followed
- [ ] All breaking changes addressed per catalog

### Validation Checklist

After upgrade completion, verify:

1. **Build Validation**
   - `dotnet build` succeeds
   - No compilation errors
   - No new compilation warnings

2. **Test Validation**
   - `dotnet test` runs all tests
   - All unit tests pass
   - All architecture tests pass
   - All integration tests pass

3. **Runtime Validation**
   - API project starts successfully
   - Database migrations work (if applicable)
   - Basic API endpoints respond correctly

4. **Docker Validation** (if applicable)
   - Docker build succeeds
   - Container runs correctly
