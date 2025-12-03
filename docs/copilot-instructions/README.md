
00-OVERVIEW.md:
High-level summary of the architecture (Modular Monolith, DDD, CQRS).
Explanation of the module structure (Domain, Application, Infrastructure).
Key technology stack details.


01-NEW-FEATURE-GUIDE.md:
Step-by-step guide for adding new features.
Distinction between Commands (Write Model) and Queries (Read Model).
Code snippets for Command Handlers, Validators, and Query Handlers using Dapper.


02-NEW-MODULE-GUIDE.md:

Instructions for scaffolding a new module from scratch.
Required folder structure and project dependencies.
How to register the module with the API and Event Bus.


03-TESTING-GUIDELINES.md:

Rules for Unit Tests (Domain focus, no mocks for domain objects).
Rules for Integration Tests (Module level, real database).
Overview of Architecture and System Integration tests.


04-DATABASE-CHANGES.md:

Workflow for managing database schemas using DbUp and SSDT.
Naming conventions for migration scripts.


05-EVENT-SOURCING-GUIDE.md:

Specific instructions for the Payments module which uses Event Sourcing.
How to implement Event-Sourced Aggregates and Projections.


You can now reference these files when asking Copilot to perform tasks, for example: "Copilot, add a new command to the Meetings module following the steps in 01-NEW-FEATURE-GUIDE.md."