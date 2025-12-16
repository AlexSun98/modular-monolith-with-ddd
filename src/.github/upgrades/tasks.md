# CompanyName.MyMeetings .NET 10.0 Upgrade Tasks

## Overview

Atomic upgrade of the CompanyName.MyMeetings Modular Monolith solution from .NET 8.0 to .NET 10.0 (LTS). All 47 projects will be upgraded simultaneously in a single coordinated operation, followed by comprehensive testing and validation.

**Progress**: 0/3 tasks complete (0%) ![0%](https://progress-bar.xyz/0)

---

## Tasks

### [▶] TASK-001: Verify prerequisites
**References**: Plan §Executive Summary - Target State

- [▶] (1) Verify .NET 10.0 SDK installed on build environment
- [ ] (2) .NET 10.0 SDK available and functional (**Verify**)
- [ ] (3) Check for global.json file in repository root (if present, update to SDK 10.0)
- [ ] (4) global.json compatible with .NET 10.0 or not restricting SDK version (**Verify**)

---

### [ ] TASK-002: Atomic framework and dependency upgrade with compilation fixes
**References**: Plan §Project-by-Project Plans, Plan §Package Update Reference, Plan §Breaking Changes Catalog

- [ ] (1) Update TargetFramework from `net8.0` to `net10.0` in all 47 projects per Plan §Project-by-Project Plans (excluding Database.Build which stays netstandard2.1)
- [ ] (2) All applicable project files updated to net10.0 (**Verify**)
- [ ] (3) Update package references per Plan §Package Update Reference: Microsoft.EntityFrameworkCore 8.0.0→10.0.1, Microsoft.EntityFrameworkCore.SqlServer 8.0.0→10.0.1 (BuildingBlocks.Infrastructure), Newtonsoft.Json 13.0.3→13.0.4 (BuildingBlocks.Application)
- [ ] (4) All package references updated (**Verify**)
- [ ] (5) Update or remove Microsoft.VisualStudio.Azure.Containers.Tools.Targets package in API project per Plan §Package Update Reference
- [ ] (6) Container tools package resolved (**Verify**)
- [ ] (7) Restore all NuGet packages: `dotnet restore`
- [ ] (8) All packages restored successfully with no conflicts (**Verify**)
- [ ] (9) Build entire solution: `dotnet build`
- [ ] (10) Fix TimeSpan.FromSeconds/FromMilliseconds compilation errors per Plan §Breaking Changes Catalog §1 (cast integer arguments to double: `TimeSpan.FromSeconds(1.0)`) in 5 Module Infrastructure projects
- [ ] (11) Fix Rfc2898DeriveBytes compilation errors per Plan §Breaking Changes Catalog §3 in Payments.Application (explicitly specify hash algorithm if needed)
- [ ] (12) Address SqlConnection API warnings per Plan §Breaking Changes Catalog §2 in test projects (suppress warnings or migrate to Microsoft.Data.SqlClient if necessary)
- [ ] (13) Rebuild solution: `dotnet build`
- [ ] (14) Solution builds with 0 errors (**Verify**)
- [ ] (15) Commit changes with message: "TASK-002: Upgrade all projects to .NET 10.0 and update dependencies"

---

### [ ] TASK-003: Execute comprehensive test suite and validate upgrade
**References**: Plan §Testing Strategy, Plan §Success Criteria

- [ ] (1) Run all unit tests: BuildingBlocks.Application.UnitTests and 5 Module Domain UnitTests projects
- [ ] (2) Run all architecture tests: Solution ArchTests and 5 Module ArchTests projects
- [ ] (3) Run all integration tests: BuildingBlocks.IntegrationTests, Solution IntegrationTests, and 5 Module IntegrationTests projects
- [ ] (4) Run SUT test project: CompanyName.MyMeetings.SUT
- [ ] (5) Fix any test failures related to framework behavioral changes per Plan §Breaking Changes Catalog (focus on SqlConnection changes in integration tests, TimeSpan API changes in retry policy tests, Environment.SetEnvironmentVariable in build tests)
- [ ] (6) Re-run all test projects after fixes
- [ ] (7) All tests pass with 0 failures (**Verify**)
- [ ] (8) Verify API project starts successfully and basic endpoints respond
- [ ] (9) API runtime validation successful (**Verify**)
- [ ] (10) Commit test fixes and validation results with message: "TASK-003: Complete .NET 10.0 upgrade testing and validation"

---
