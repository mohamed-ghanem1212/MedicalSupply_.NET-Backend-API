# Medical Supply Request Management System

A Clean Architecture ASP.NET Core Web API for managing medical/administrative
supply requests: creation, multi-stage approval, stock reservation, cancellation,
and fulfillment.

> **Before you run this:** this solution was generated without access to a .NET
> SDK, so it has **not** been compiled. Follow "Getting it running" below, and
> see "Known limitations" for what to expect on the first build.

---

## 1. Project overview

Departments (Pharmacy, Clinics, Nursing, Laboratories, Finance, Administration)
submit requests for inventory items. Each request goes through a dynamically
determined approval flow (Department Manager, optionally Pharmacy, optionally
Finance), only reserves stock once fully approved, and is fulfilled by a Store
Keeper — reducing both reserved and available quantity at that point.

## 2. Architecture

```
MedicalSupply.sln
src/
  MedicalSupply.Domain/            entities, enums, domain exceptions, the approval state machine
  MedicalSupply.Application/       use-case services, DTOs, abstractions, authorization
  MedicalSupply.Infrastructure/    EF Core, repositories, JWT, seed data
  MedicalSupply.Api/               controllers, JWT middleware, Swagger, global exception handling
```

```
 MedicalSupply.Api
        │  (controllers, HTTP contracts, JWT wiring)
        ▼
 MedicalSupply.Application  ◄────────┐
        │  (use cases, interfaces)   │ implements
        ▼                            │
 MedicalSupply.Domain         MedicalSupply.Infrastructure
 (entities, invariants)       (EF Core, JWT, repositories)
```

Dependency direction: `Api → Application → Domain`, and
`Infrastructure → Application + Domain` (Infrastructure *implements* interfaces
defined in Application — it is never referenced *by* Application). Domain has
zero project references. This mirrors the assessment's Section 9.4 exactly.

### Why a plain service layer instead of MediatR/CQRS
The spec explicitly says MediatR, CQRS, and the generic Repository pattern are
optional and "must not be added solely to imitate a template." Given the
24-hour time-box, four coarse-grained per-aggregate services
(`DepartmentService`, `ItemService`, `SupplyRequestService`, `AuthService`) hit
every required use case with far less indirection than a full command/handler
pipeline, while still keeping Application decoupled from EF Core and ASP.NET
Core — the actual thing Clean Architecture is graded on here.

## 3. Getting it running

**Requirements:** .NET 8 SDK (or later), no external database needed by default
(SQLite is the out-of-the-box provider).

```bash
cd MedicalSupply
dotnet restore

# 1. Generate the initial migration (not included — see "Known limitations")
cd src/MedicalSupply.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../MedicalSupply.Api

# 2. Run the API — it applies the migration and seeds data automatically on startup
cd ../MedicalSupply.Api
dotnet run
```

Swagger UI opens at `https://localhost:<port>/swagger`.

### Switching to SQL Server
Edit `src/MedicalSupply.Api/appsettings.json`:
```json
{
  "Database": { "Provider": "SqlServer" },
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=MedicalSupplyDb;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```
Then re-run `dotnet ef migrations add InitialCreate` (EF Core migrations are
provider-specific — you'll want to add a SqlServer migration, or maintain
separate migration folders if you need both providers long-term).

### JWT signing key
`appsettings.json` ships a placeholder `Jwt:SigningKey`. For anything beyond a
local demo, override it via `dotnet user-secrets` or an environment variable
rather than committing a real secret:
```bash
dotnet user-secrets set "Jwt:SigningKey" "<a long random string>"
```

## 4. Authentication & sample users

All demo users share the password **`Passw0rd!`**.

| Email | Role |
|---|---|
| requester@company.com | Requester |
| manager@company.com | DepartmentManager |
| pharmacist@company.com | Pharmacist |
| finance@company.com | FinanceOfficer |
| storekeeper@company.com | StoreKeeper |
| admin@company.com | Administrator |

```
POST /api/auth/login
{ "email": "requester@company.com", "password": "Passw0rd!" }
```
Copy the returned `accessToken` into Swagger's **Authorize** button (just the
token — Swagger prepends `Bearer ` for you).

## 5. Business-rule walkthrough

- **Creation** (`POST /api/supply-requests`): validates department/items are
  active, no duplicate items, quantities > 0, snapshots each item's current
  unit price, computes line and request totals, generates a unique request
  number (`SR-2026-000001` format).
- **Submission** (`.../submit`): computes the required approval flow —
  Department Manager always; Pharmacy if any item requires it or is a
  controlled medication; Finance if total > 10,000 — and rejects submission if
  the total exceeds the department's remaining monthly budget.
- **Approval** (`.../approve`, `.../reject`): only the role matching the
  pending approval type may act (Pharmacist for Pharmacy, etc.); the decision
  is attributed to the **authenticated caller**, not the request body's
  `decisionBy` field (spec Section 7's explicit warning). When the last
  required approval is accepted, stock is re-validated and reserved for every
  line item inside one transaction; if any item now lacks sufficient stock,
  the whole approval fails and nothing changes.
- **Cancellation** (`.../cancel`): allowed from Draft through Approved, not
  from Fulfilled or Rejected; cancelling an Approved request releases all
  reserved stock atomically.
- **Fulfillment** (`.../fulfill`): only from Approved; reduces both Reserved
  and Available quantity together in one transaction; re-fulfilling an already
  Fulfilled request is blocked by the Domain's state check.

## 6. Transactions

Every operation that touches more than one aggregate (approval's stock
reservation, cancellation's stock release, fulfillment's stock reduction) runs
inside `IUnitOfWork.ExecuteInTransactionAsync` (`Infrastructure/Persistence/UnitOfWork.cs`),
which begins a DB transaction, runs the use case (including `SaveChangesAsync`),
and commits — or rolls back on **any** exception, domain or infrastructure.
Single-aggregate writes (creating a department, updating an item) just use
`SaveChangesAsync` directly since there's nothing to roll back atomically with.

## 7. Concurrency handling

`Item.Version` (`Domain/Entities/Item.cs`) is a manually-incremented `int`,
configured as an EF Core concurrency token
(`Infrastructure/Persistence/Configurations/ItemConfiguration.cs`). Every
`UPDATE Items ...` statement EF Core generates includes
`WHERE Id = @id AND Version = @originalVersion`. If two approvals race to
reserve the same item, the second one to commit finds zero rows matched, EF
throws `DbUpdateConcurrencyException`, and `UnitOfWork` translates that into a
409 `CONCURRENCY_CONFLICT` — the caller retries, re-reading the now-current
stock. A manually incremented `int` was chosen over SQL Server's native
`rowversion` column type specifically because the spec allows **either** SQL
Server or SQLite, and SQLite has no equivalent auto-updating rowversion type;
an ordinary concurrency-token column works identically on both.

## 8. Request-number uniqueness

`RequestNumberGenerator` (`Infrastructure/Services/RequestNumberGenerator.cs`)
computes `SR-{year}-{count-of-this-year's-requests + 1}`, and the database has
a **unique index** on `RequestNumber`
(`SupplyRequestConfiguration.cs`). If two requests are created in the same
instant and would compute the same number, the loser's `SaveChangesAsync`
throws a unique-constraint violation instead of silently duplicating — the
count-based guess makes collisions rare, the unique index makes them
impossible to persist.

## 9. Preventing partial updates

Every multi-step business operation is wrapped by `ExecuteInTransactionAsync`
(see Section 6) — an exception anywhere in the delegate rolls back everything
attempted so far, so the API never leaves a request "Approved" with stock not
actually reserved, or vice versa.

## 10. Assumptions

- "Only a request in Draft status can be submitted" — items can only be added
  to a request while it's Draft (spec 5.1 doesn't specify a separate
  add-item endpoint, so items are supplied at creation time only).
- Partial approval quantities (`ApprovedQuantity` differing from
  `RequestedQuantity`) aren't required by the spec; `ApprovedQuantity` is set
  equal to `RequestedQuantity` when a request becomes Approved.
- Cancellation authorization: the spec doesn't name a role for this operation,
  so it's restricted to `Requester` and `DepartmentManager` (plus
  Administrator) as the most plausible owners of that decision.
- "Remaining monthly budget" is computed as `MonthlyBudget` minus the sum of
  `TotalAmount` for all of that department's requests **except** Rejected and
  Cancelled ones (i.e., Draft/Submitted/Pending*/Approved/Fulfilled all still
  commit against budget). There's no explicit "reset budget monthly" job —
  see Known limitations.
- Fulfillment quantity uses `ApprovedQuantity` (falling back to
  `RequestedQuantity` if unset), matching spec 5.7.

## 11. Known limitations / unfinished items

- **No EF Core migration is checked in.** Without SDK access in the
  environment this was built in, `dotnet ef migrations add` could not be run.
  This is the single most important thing to do before anything else — see
  Section 3.
- **No automated build verification.** The code has not been compiled;
  expect the possibility of small issues (a missing `using`, a mismatched
  generic constraint) on the first `dotnet build`, given the volume of code
  written without a compiler in the loop.
- **Demo password hashing** (`DemoUserDirectory`) is a bare SHA-256 hash with
  no salt or iteration — adequate only for a hardcoded demo list, not a
  pattern to reuse for real user storage (would use ASP.NET Core Identity /
  a proper password hasher with salting and iteration counts in production).
  All seeded users share the password `Passw0rd!`.
- **No refresh tokens** — access tokens simply expire after 120 minutes
  (configurable), matching the spec's "optional" note on refresh tokens.
- **No monthly budget reset/rollover job** — `MonthlyBudget` is a static cap
  per department; a real system would need a scheduled job or period-aware
  query to reset "committed amount" at each month boundary.
- **No idempotency keys, outbox pattern, or message queue** — all explicitly
  optional per spec Section 16, intentionally skipped to protect time spent on
  mandatory scope.
- **Logging** uses the built-in `ILogger` (structured, includes trace IDs) but
  doesn't ship a dedicated audit-log table beyond the `ApprovalRecords` table
  itself, which is already immutable and serves as the approval audit trail.

## 12. Postman collection

See `MedicalSupply.postman_collection.json` in the repository root — covers
login for each role, department/item CRUD, and the full request lifecycle
(create → submit → approve × N → fulfill, plus a reject and a cancel branch).

## 13. Technical decisions & trade-offs

- **Manually incremented concurrency token vs `rowversion`**: portability
  across SQL Server/SQLite, discussed in Section 7.
- **Coarse per-aggregate services vs MediatR/CQRS**: discussed in Section 2.
- **Decision attribution from JWT, not request body**: the spec explicitly
  warns against trusting `decisionBy`; `SupplyRequestService` uses
  `ICurrentUserService.Email` for the stored `DecisionBy`, and role checks are
  performed against the same source — the request body's `decisionBy` field
  is accepted for wire-compatibility with the spec's example payloads but
  never used to authorize or attribute anything.
- **SQLite as the default provider**: zero external dependencies for a
  reviewer to get the API running immediately; SQL Server remains a one-line
  config change away.
