# Requirements: Payroll Pay Run Module (Phase 1)

> **Source**: Clean-room rebuild of `PayrollPayrunsController` from `reckonone-apps-2026`
> **Scope**: Employee Master Profile + Core Pay Run lifecycle + Employee Pay Items + Tax Transactions + Leave Summary
> **Out of Scope**: STP submissions, EOFY reports, payslip/report generation, SuperStream, YTD data management

---

## Requirement 1: Pay Run Lifecycle Management

**User Story**: As a payroll administrator, I want to create, view, update, finish, undo, and delete pay runs, so that I can manage the full pay cycle for a book.

### Acceptance Criteria

- **Given** a valid BookId, **when** I request a list of pay runs, **then** I receive paginated pay run summaries (name, status, pay period dates, employee count, total gross, total net).
- **Given** a valid BookId and pay period details, **when** I create a pay run draft, **then** a new pay run is created in `Draft` status with employees loaded based on the specified loading strategy (CopyFromMaster, CopyFromPreviousPay, CopyFromCustomData, TimeEntry).
- **Given** a pay run in `Draft` status, **when** I batch-update pay run details, **then** the pay run name, dates, and employee data are updated atomically.
- **Given** a pay run in `Draft` status, **when** I finish the pay run, **then** the status transitions to `Finished`, all employee pays are locked, and a domain event is raised.
- **Given** a pay run in `Finished` status, **when** I undo the pay run, **then** the status reverts to `Draft`, employee pays are unlocked, and a domain event is raised.
- **Given** a pay run in `Draft` status with no downstream dependencies, **when** I delete the pay run, **then** the pay run and all associated employee pay data are removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to delete it, **then** the operation is rejected with a business rule violation.

### Breaking Changes from Legacy API

| Legacy | New (DDD) | Reason |
|--------|-----------|--------|
| `GET /{cashbookId}/payroll/payruns` returns OData `$skip/$top` | `GET /payroll/{bookId}/payruns?page=1&pageSize=20` returns cursor/offset pagination | DDD uses explicit pagination, not OData |
| `PATCH /{cashbookId}/payroll/payruns/{payrunId}/batch` | `PUT /payroll/{bookId}/payruns/{payrunId}` | REST convention: PUT for full update |
| `POST .../finish` and `POST .../undo` | `POST /payroll/{bookId}/payruns/{payrunId}/finish` and `POST .../undo` | Kept as-is (state transition commands) |

---

## Requirement 2: Employee Earnings and Leave Management

**User Story**: As a payroll administrator, I want to add, update, and remove earnings and leave items for each employee in a pay run, so that employee pay is calculated correctly.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create an earning or leave item with pay item ID, quantity, rate, and optional loading percentage, **then** the item is added and the employee's gross pay is recalculated.
- **Given** an existing earning or leave item, **when** I update its quantity, rate, or loading, **then** the item is updated and the employee's gross pay is recalculated.
- **Given** an existing earning or leave item, **when** I delete it, **then** the item is removed and the employee's gross pay is recalculated.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify earnings or leave, **then** the operation is rejected with a business rule violation.

---

## Requirement 3: Allowance Management

**User Story**: As a payroll administrator, I want to add, update, and remove allowance items for each employee in a pay run, so that non-standard payments are tracked separately for reporting and STP.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create an allowance with pay item ID, rate, and quantity, **then** the allowance is added to the employee's pay.
- **Given** an existing allowance, **when** I update its rate or quantity, **then** the allowance is updated.
- **Given** an existing allowance, **when** I delete it, **then** the allowance is removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify allowances, **then** the operation is rejected.

---

## Requirement 4: Deduction Management

**User Story**: As a payroll administrator, I want to add, update, and remove deduction items for each employee in a pay run, so that pre-tax and post-tax deductions are applied correctly.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create a deduction with pay item ID, rate, quantity, and optional payee, **then** the deduction is added to the employee's pay.
- **Given** an existing deduction, **when** I update its rate, quantity, or payee, **then** the deduction is updated.
- **Given** an existing deduction, **when** I delete it, **then** the deduction is removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify deductions, **then** the operation is rejected.

---

## Requirement 5: Reimbursement Management

**User Story**: As a payroll administrator, I want to add, update, and remove reimbursement items for each employee in a pay run, so that expense reimbursements are included in pay and reported correctly.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create a reimbursement with pay item ID, rate, quantity, and tax indicator, **then** the reimbursement is added.
- **Given** an existing reimbursement, **when** I update its rate, quantity, or tax indicator, **then** the reimbursement is updated.
- **Given** an existing reimbursement, **when** I delete it, **then** the reimbursement is removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify reimbursements, **then** the operation is rejected.

---

## Requirement 6: Superannuation Management

**User Story**: As a payroll administrator, I want to add, update, remove, and bulk-copy superannuation contributions for each employee in a pay run, so that employer and employee super obligations are met.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create a super contribution with pay item ID, rate, type, and optional company super fund ID, **then** the super item is added.
- **Given** an existing super item, **when** I update its rate, type, or fund, **then** the super item is updated.
- **Given** an existing super item, **when** I delete it, **then** the super item is removed.
- **Given** an employee in a draft pay run, **when** I copy all super items from the employee's master profile, **then** the employee's pay run super items are replaced with the current profile configuration.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify super items, **then** the operation is rejected.

---

## Requirement 7: Company Contribution Management

**User Story**: As a payroll administrator, I want to add, update, and remove company contribution items for each employee in a pay run, so that additional employer-paid benefits are tracked.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I create a company contribution with pay item ID, rate, and quantity, **then** the contribution is added.
- **Given** an existing company contribution, **when** I update its rate or quantity, **then** the contribution is updated.
- **Given** an existing company contribution, **when** I delete it, **then** the contribution is removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify company contributions, **then** the operation is rejected.

---

## Requirement 8: Tax Transaction Management

**User Story**: As a payroll administrator, I want to override gross earnings tax and manage termination payment (ETP) tax entries for employees in a pay run, so that tax withholding is correct for non-standard situations.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I override the gross earnings tax amount, **then** the system uses the overridden amount instead of the calculated value.
- **Given** an employee in a draft pay run, **when** I create a termination payment tax entry with taxable component, tax-free component, and ETP code, **then** the entry is added.
- **Given** an existing termination tax entry, **when** I update its components or code, **then** the entry is updated.
- **Given** an existing termination tax entry, **when** I delete it, **then** the entry is removed.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify tax transactions, **then** the operation is rejected.

---

## Requirement 9: Leave Balance Summary

**User Story**: As a payroll administrator, I want to view and override the leave balance summary for each employee in a pay run, so that leave accruals are accurate.

### Acceptance Criteria

- **Given** an employee in a pay run, **when** I request their leave summary, **then** I receive the current balances for all leave categories (annual, personal, long service, etc.) including opening balance, accrued this pay, taken this pay, and closing balance.
- **Given** an employee in a draft pay run, **when** I override the "accumulated this pay" value for a leave category, **then** the override is applied and the closing balance is recalculated.
- **Given** a pay run not in `Draft` status, **when** I attempt to override leave accruals, **then** the operation is rejected.

---

## Requirement 10: Employee Pay Run Header Management

**User Story**: As a payroll administrator, I want to update an employee's reporting classification and archivable status within a pay run, so that employee metadata is correct for the current pay cycle.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I update their reporting classification, **then** the classification is saved for this pay run.
- **Given** an employee in a draft pay run, **when** I mark them as archivable, **then** the employee is flagged for potential archival after the pay run is finished.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify employee header data, **then** the operation is rejected.

---

## Requirement 11: Pay Frequency Management

**User Story**: As a payroll administrator, I want to update the pay frequency for an employee within a pay run, so that pro-rata calculations use the correct frequency.

### Acceptance Criteria

- **Given** an employee in a draft pay run, **when** I update their pay frequency (weekly, fortnightly, monthly, etc.), **then** the frequency is saved and any frequency-dependent calculations are recalculated.
- **Given** a pay run not in `Draft` status, **when** I attempt to modify pay frequency, **then** the operation is rejected.

---

## Requirement 12: Non-Functional — Multi-Tenancy and Future Compatibility

**User Story**: As a system architect, I want the Payroll module to use BookId-based multi-tenancy and maintain future compatibility with the legacy Reckon Payroll database schema and auth, so that migration from the legacy system is feasible.

### Acceptance Criteria

- **Given** the Payroll module, **when** any API endpoint is called, **then** the BookId is required and all data is scoped to that book — no cross-book data access is possible.
- **Given** the module's database schema, **when** tables are designed, **then** column names and types should align with the legacy Reckon Payroll schema where practical to enable future data migration.
- **Given** the module's domain model, **when** entities reference employees or pay items, **then** IDs should use the same underlying types (Guid) as the legacy system to enable future integration.

---

## Requirement 13: Non-Functional — DDD Module Standards Compliance

**User Story**: As a developer, I want the Payroll module to follow all DDD modular monolith conventions (Clean Architecture layers, CQRS, domain events, Autofac composition root, Dapper for reads, EF Core for writes), so that the module is consistent with the rest of the codebase.

### Acceptance Criteria

- **Given** the module structure, **when** the module is created, **then** it follows the standard 4-project layout: Domain, Application, Infrastructure, IntegrationEvents.
- **Given** any write operation, **when** a command is executed, **then** business logic lives in Domain Aggregates/Entities (not in handlers or services).
- **Given** any read operation, **when** a query is executed, **then** it uses Dapper against database views and returns flat DTOs (never domain objects).
- **Given** the module, **when** it communicates with other modules, **then** it uses Integration Events via the Event Bus (no direct method calls).
- **Given** any state transition (finish, undo), **when** it occurs, **then** domain events are raised and can trigger side effects.

---

## Requirement 14: Employee Profile Management

**User Story**: As a payroll administrator, I want to create, view, update, and list employees (payroll contacts) for a book, so that I have a master employee register for pay runs.

### Acceptance Criteria

- **Given** a valid BookId, **when** I request a list of employees, **then** I receive a paginated list of employee summaries (ID, name, employee number, employment type, status, pay schedule).
- **Given** a valid BookId and employee details, **when** I create a new employee, **then** the employee is created with default employment setup and a domain event is raised.
- **Given** an existing employee, **when** I request their full profile, **then** I receive complete personal, employment, financial, leave, and tax data.
- **Given** an existing employee, **when** I update their profile, **then** the relevant sections are updated and domain events are raised for any changes that affect unpaid pay runs.

### Breaking Changes from Legacy API

| Legacy | New (DDD) | Reason |
|--------|-----------|--------|
| `GET /{cashbookId}/payroll/contacts` | `GET /payroll/{bookId}/employees` | "Contacts" renamed to "Employees" — domain-aligned naming |
| `POST /{cashbookId}/payroll/contacts` with full nested payload | `POST /payroll/{bookId}/employees` creates base profile only; sub-resources updated separately | Aggregate boundary — employee creation shouldn't require financial/tax/leave setup in one call |
| `PUT /{cashbookId}/payroll/contacts/{contactId}` updates everything | Separate PUT endpoints per sub-resource (personal, employment, financial, tax, leave) | Smaller aggregates, clearer intent |

---

## Requirement 15: Employee Personal Information

**User Story**: As a payroll administrator, I want to view and update an employee's personal information (date of birth, gender, marital status, emergency contacts, communication preferences), so that employee records are complete for compliance and payslip delivery.

### Acceptance Criteria

- **Given** an existing employee, **when** I request their personal info, **then** I receive DOB, gender, marital status, disability flag, email, emergency contacts, and communication preferences.
- **Given** an existing employee, **when** I update their personal info, **then** the changes are saved and a domain event is raised.
- **Given** an update with an invalid email format, **when** submitted, **then** the operation is rejected with a validation error.

---

## Requirement 16: Employee Employment Details

**User Story**: As a payroll administrator, I want to manage an employee's employment details (job title, hire date, pay schedule, base rate, employment type, award/agreement classification, termination, rehire), so that pay calculations use the correct employment context.

### Acceptance Criteria

- **Given** an existing employee, **when** I request their employment details, **then** I receive employee number, job title, department, employment type, pay frequency, weekly hours, base rate, pay schedule, award/agreement classification, important dates (hire, long service, termination), and payment methods.
- **Given** an existing employee, **when** I update their employment details, **then** the changes are saved and domain events are raised.
- **Given** an existing employee, **when** I update their base rate, **then** the rate is updated and any unpaid pay runs are flagged for recalculation.
- **Given** a terminated employee with a valid rehire date, **when** I rehire them, **then** a new hire date is recorded, termination status is cleared, and a domain event is raised.
- **Given** an employee, **when** I apply a pay template, **then** the template's pay items (earnings, allowances, deductions, super) are copied to the employee's financial setup, with an option to keep or replace existing items.
- **Given** an employee with a pay template applied, **when** I remove the pay template, **then** the template link is removed but the existing pay items remain.

### Breaking Changes from Legacy API

| Legacy | New (DDD) | Reason |
|--------|-----------|--------|
| `POST .../employment/paytemplate/apply` | `POST /payroll/{bookId}/employees/{employeeId}/pay-template/apply` | Kebab-case routes |
| `POST .../rehire` | `POST /payroll/{bookId}/employees/{employeeId}/rehire` | Same pattern, new base path |
| Base rate update at `PUT .../employment/baseRate` | `PUT /payroll/{bookId}/employees/{employeeId}/base-rate` | Dedicated sub-resource |

---

## Requirement 17: Employee Financial Setup (Master Pay Items)

**User Story**: As a payroll administrator, I want to view and manage an employee's master pay item configuration (earnings, allowances, deductions, super accounts, company contributions), so that new pay runs are pre-populated with the correct items.

### Acceptance Criteria

- **Given** an existing employee, **when** I request their financial setup, **then** I receive their configured earnings (pay item, qty, rate, rate basis, customer, project, is base rate), allowances (pay item, qty, rate, calc basis, limit), deductions (pay item, qty, rate, payee, rate type, limit), super accounts (pay item, rate, rate type, fund, product, reference, join date, statutory rate flag), and company contributions (pay item, qty, rate, calc basis, limit, payee).
- **Given** an existing employee, **when** I update their financial setup, **then** individual pay items can be added, updated, or removed within each category.
- **Given** a pay item with a limit configured, **when** the accumulated amount across pay runs exceeds the limit, **then** the `HasExceededLimit` flag is set to true on the next query.
- **Given** a financial setup change on an employee, **when** the change is saved, **then** unpaid pay runs referencing that employee are flagged for potential recalculation.

### Breaking Changes from Legacy API

| Legacy | New (DDD) | Reason |
|--------|-----------|--------|
| `GET/PUT .../paysetup/{contactId}` replaces entire financial setup in one call | Separate endpoints per pay item category with individual CRUD | Finer-grained operations; avoids replacing 5 collections atomically |

---

## Requirement 18: Employee Leave Configuration

**User Story**: As a payroll administrator, I want to manage an employee's leave types (annual, personal, long service, etc.) and view their leave balances, so that leave accruals and entitlements are correctly tracked.

### Acceptance Criteria

- **Given** an existing employee, **when** I request their leave types, **then** I receive all configured leave items with entitlement, accumulation rate, start date, accrual period, maximum cap, loading percentage, and pay-on-termination flag.
- **Given** an existing employee, **when** I create a new leave type, **then** the leave configuration is added.
- **Given** an existing leave type, **when** I update it, **then** the configuration is saved.
- **Given** an existing leave type with no usage history, **when** I delete it, **then** the leave type is removed.
- **Given** an existing employee, **when** I request their leave balances, **then** I receive accrued, used, available, and projected balances with dollar values for each leave type.

---

## Requirement 19: Employee Tax Details

**User Story**: As a payroll administrator, I want to view and update an employee's tax configuration (TFN, tax scale, residency status, HELP debt, medicare levy, withholding variations), so that PAYG withholding is calculated correctly.

### Acceptance Criteria

- **Given** an existing employee, **when** I request their tax details, **then** I receive TFN (masked for display), tax scale, residency type, HELP debt flag, voluntary flat rate, tax offset, state, income stream code, medicare details, and withholding variation details.
- **Given** an existing employee, **when** I update their tax details, **then** the changes are saved and a domain event is raised (tax changes may affect unpaid pay run calculations).
- **Given** a TFN update, **when** saved, **then** the TFN is stored securely (the domain does not expose raw TFN in query results — only masked format).
- **Given** an employee with `IsResident = false` and no valid `HomeCountryCode`, **when** I submit the update, **then** the operation is rejected with a validation error.
