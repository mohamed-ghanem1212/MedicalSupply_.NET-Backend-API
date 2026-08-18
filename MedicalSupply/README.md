# Medical Supply Request Management System

A small ASP.NET Core Web API for requesting, approving, and fulfilling
medical/administrative supply requests, built with a 4-project Clean
Architecture structure.

> This was rewritten as a **deliberately simplified** version. See
> "Simplifications made" below for what was cut compared to a full
> implementation of every rule in the assessment spec, and why.

---

## 1. What it does

A department (Pharmacy, Clinics, Nursing, Labs) creates a **supply
request** for one or more inventory items. The request goes through one
approval step, and only once approved does the system reserve stock for
it. A Store Keeper then fulfills it, which is when stock is actually
deducted.

Flow: `Draft → Submitted → Approved/Rejected → Fulfilled`, with `Cancel`
available from Draft, Submitted, or Approved.

## 2. Architecture

```
MedicalSupply.sln
src/
  MedicalSupply.Domain/          entities, enums, exceptions - no dependencies
  MedicalSupply.Application/     services (business logic), DTOs, interfaces
  MedicalSupply.Infrastructure/  EF Core DbContext, JWT, seed data
  MedicalSupply.Api/             controllers, JWT setup, exception middleware
```

```
 MedicalSupply.Api
        |
        v
 MedicalSupply.Application  <--------+
        |                            | implements
        v                            |
 MedicalSupply.Domain          MedicalSupply.Infrastructure
```

**Dependency direction:** `Api -> Application -> Domain`, and
`Infrastructure -> Application + Domain`. Domain has no project
references at all. Infrastructure implements the `IAppDbContext`
interface that Application defines - Application never references
Infrastructure directly, which is the actual point of Clean Architecture
(the inner layers don't know about the outer ones).

### Why no Repository/UnitOfWork pattern here

Instead of a repository per entity plus a separate unit-of-work class,
Application just depends on one small interface, `IAppDbContext`
(`Application/Abstractions/IAppDbContext.cs`), which exposes the
`DbSet<T>` properties it needs. Services use normal LINQ against it,
the same way you'd use a `DbContext` directly. `Infrastructure`'s real
`AppDbContext` implements that interface. This keeps the dependency
direction correct (Application still doesn't reference EF Core's
`DbContext` class or Infrastructure) without the extra files a full
repository layer would add. Good enough for four entities and a handful
of use cases - a much bigger domain might outgrow this.

## 3. Getting it running

Requires the .NET 8 SDK.

```bash
cd MedicalSupply
dotnet restore

cd src/MedicalSupply.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../MedicalSupply.Api

cd ../MedicalSupply.Api
dotnet run
```

Swagger UI: `http://localhost:5000/swagger` (or whatever port the console
output shows). The API applies the migration and seeds sample data
automatically on startup.

### Switching to SQL Server

In `appsettings.json`:

```json
"Database": { "Provider": "SqlServer" },
"ConnectionStrings": { "DefaultConnection": "Server=.;Database=MedicalSupplyDb;Trusted_Connection=True;TrustServerCertificate=True" }
```

## 4. Authentication

Four hardcoded demo users, one per role, all using password `Passw0rd!`:

| Email                   | Role              |
| ----------------------- | ----------------- |
| requester@company.com   | Requester         |
| manager@company.com     | DepartmentManager |
| storekeeper@company.com | StoreKeeper       |
| admin@company.com       | Administrator     |

```
POST /api/auth/login
{ "email": "requester@company.com", "password": "Passw0rd!" }
```

Paste the returned `token` into Swagger's **Authorize** button.

## 5. RBAC - who can do what

| Action                           | Allowed roles                                         |
| -------------------------------- | ----------------------------------------------------- |
| Create/update department or item | Administrator                                         |
| Create/submit/cancel a request   | Requester (cancel also allowed for DepartmentManager) |
| Approve/reject a request         | DepartmentManager                                     |
| Fulfill a request                | StoreKeeper                                           |
| Administrator                    | can do everything                                     |

Enforced with `[Authorize(Roles = "...")]` on each controller action.

## 6. Validation & error handling

Services throw one of four exceptions
(`Domain/Exceptions/AppExceptions.cs`): `NotFoundException`,
`ValidationException`, `ConflictException`, `ForbiddenException`.
`ExceptionHandlingMiddleware` (in the Api project) catches all of them
in one place and returns a consistent JSON body:

```json
{ "code": "VALIDATION_ERROR", "message": "...", "traceId": "..." }
```

Anything not one of those four falls through to a generic 500 so
internal details are never leaked to the client.

## 7. Concurrency handling

`Item.Version` is an `int` that's incremented every time stock changes
(reserve/release/fulfill), and it's marked as an EF Core concurrency
token in `AppDbContext`. If two requests try to reserve the same item's
stock at the same time, the second one to save gets a
`DbUpdateConcurrencyException`, which `SupplyRequestService.ApproveAsync`
catches and turns into a 409 Conflict asking the caller to retry. A
plain incrementing `int` was used instead of SQL Server's `rowversion`
column type specifically because it works the same way on SQLite too.

## 8. Incomplete requirements and assumptions

To keep the codebase something I can fully explain and defend, this
version deliberately does less than a full implementation of every rule
in the assessment spec. Listed as the doc's submission instructions ask:

### Incomplete requirements (spec rules not implemented)

- **Only one approval step**, not the spec's three-stage flow. Every
  request just needs Department Manager approval — there's no separate
  Pharmacy approval or Finance approval stage, and no logic to decide
  which stages a request needs.
- **No monthly budget enforcement.** `Department.MonthlyBudget` exists
  as a field but request submission does not check the request total
  against it, and submission is never rejected for exceeding it.
- **No approval audit table.** The spec calls for a separate
  `ApprovalRecords` table with one row per decision; this version stores
  the single decision directly on the request instead
  (`DecisionBy`, `DecisionDate`, `RejectionReason`).
- **No partial-approval quantities.** The spec allows an approved
  quantity to differ from the requested quantity; here the approved
  quantity always equals the requested quantity.
- **No Pharmacist or FinanceOfficer roles.** Since there's no separate
  approval stage for either, those roles from the spec's role table
  aren't implemented.

### Assumptions (where the spec was ambiguous and a judgment call was made)

- **Cancellation authorization**: the spec doesn't name a role for this
  operation. Allowed for `Requester` and `DepartmentManager` (plus
  Administrator) as the most plausible owners of that decision.
- **Fulfillment quantity** always equals the originally requested
  quantity, since there's no partial-approval concept in this version.
- **"Remaining budget"** isn't tracked at all (see above), so there was
  no need to define what counts toward it — this is a full omission,
  not a partial interpretation.

### Deliberate code-simplicity choices (not spec gaps, just style)

- **Four exception types, not a full hierarchy** —
  `NotFoundException`, `ValidationException`, `ConflictException`,
  `ForbiddenException` cover every case instead of a specific class per
  error.
- **Entities are plain data classes.** Business rules (stock checks,
  status transitions) live in the service methods, not in methods on
  the entities themselves.
- **One `IAppDbContext` interface instead of a Repository/UnitOfWork
  layer** — see Section 2 above for why.

## 9. Postman collection

`MedicalSupply.postman_collection.json` covers login, department/item
CRUD, and the full request lifecycle: create → submit → approve →
fulfill, plus a reject and a cancel example.
