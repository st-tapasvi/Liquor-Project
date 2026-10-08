# Excise Track & Trace — Backend Architecture & Coding Standards

| | |
|---|---|
| **Applies to** | `ST.LiquorTNT` solution (`C:\Projects\Liquor_Application`) — ASP.NET Core Web API backend of the merged Excise platform, plus the WinForms line application in the same solution |
| **Version** | 1.2 (2026-09-25) — solution renamed to `ST.LiquorTNT.*`, structure section replaced with the real solution, `Business` project name confirmed, test projects per layer |
| **Status** | Approved — mandatory for all backend contributors |
| **Related** | `01-project-context.md`, `03-solution-structure.md`, `04-ai-coding-guide.md`, `frontend-architecture-standards.md`, `backend-overview-management.md` |

> **How to read.** **MUST / MUST NOT** = mandatory. **SHOULD** = default, deviate only with a written reason in the PR. Anything marked ⛔ fails the build.
> This document is also used as input for AI-assisted code generation. Every rule is written to be unambiguous; where a shape matters, the exact shape is given.

---

## 1. Locked decisions

| Area | Decision |
|---|---|
| Style | Modular monolith, ASP.NET Core Web API (.NET 8 LTS). Not microservices. |
| Layers | `Api → Business → Domain`; `Infrastructure` implements interfaces declared in Business (dependency inverted). **Domain declares no interfaces.** |
| Project names | `ST.LiquorTNT.<Layer>` — `Api`, `Business`, `Domain`, `Contracts`, `Infrastructure`, `Logging`, `Desktop`; React lives in `src/ST.LiquorTNT.WebApplication/` (a Visual Studio JavaScript project, `.esproj`, in the `.sln` — not a .NET project). The business layer is called **Business**, not "Application". |
| Deferred / dropped | `ST.LiquorTNT.Integration` (portal clients as a separate project) is **not created yet** — portal code starts under `Infrastructure/Portal/` and is split out only if that project earns its keep. `ST.LiquorTNT.Shared` was **dropped**: Contracts covers the wire format, `Business/Common` covers cross-cutting types. |
| Database | **MySQL 8.0.46 primary**, **SQL Server supported**. One customer runs one provider, chosen by configuration. |
| Data access | EF Core for normal work; **stored procedures, views and triggers used freely where the use case calls for them** |
| Response | Success = plain payload + HTTP status. Error = **RFC 7807 ProblemDetails**. No custom `{success,...}` envelope. |
| Versioning | **No version segment in the URL.** Every machine at a plant runs the same build; line apps send a build number and the server rejects a mismatch. |
| Line apps | Same host, separate area `/api/line`, separate auth scheme |
| Portal push | Local write first, then Outbox + background worker |
| Tests | xUnit + FluentAssertions, **one test project per layer** (`Domain.Tests`, `Business.Tests`, `Infrastructure.Tests`, `Api.Tests`, `Edge.Tests`, `Architecture.Tests`); integration tests against both providers via Testcontainers |
| Passwords | PBKDF2-SHA256, 100,000 iterations, per-user salt; lockout after 3 failed attempts (`Auth:MaxFailedAttempts`) |

---

## 2. Architecture and dependency rules

```
                    React (served from wwwroot)
                              │ HTTPS/REST
                              ▼
                    ┌───────────────────┐
                    │        API        │  controllers, middleware, auth, Swagger
                    └─────────┬─────────┘
                              ▼
                    ┌───────────────────┐
                    │     BUSINESS      │  use cases + excise capability providers
                    │                   │  declares interfaces it needs
                    └─────────┬─────────┘
                              ▼
                    ┌───────────────────┐
                    │      DOMAIN       │  entities, value objects, core rules
                    └───────────────────┘
                              ▲
                              │ implements the interfaces above
                    ┌─────────┴─────────┐
                    │  INFRASTRUCTURE   │  EF Core, MySQL/SQL Server, SP/views,
                    │                   │  portal clients, files, outbox, workers
                    └───────────────────┘
```

**The arrow from Infrastructure points upward.** Infrastructure depends on Business/Domain; never the reverse. This is what makes the database provider and the portal clients replaceable.

### Allowed references

| From | May reference |
|---|---|
| `Api` | `Business`, `Contracts`, `Logging`; `Infrastructure` **only** in `Extensions/ApiServiceExtensions.cs` for DI registration |
| `Business` | `Domain`, `Contracts` |
| `Infrastructure` | `Business`, `Domain`, `Contracts` |
| `Logging` | nothing (Serilog packages only) |
| `Desktop` (line app) | `Contracts` only — it talks to the API over HTTP, never to the database |
| `Domain` | nothing |
| `Contracts` | nothing |

⛔ Forbidden and enforced by `ST.LiquorTNT.Architecture.Tests` (NetArchTest, `LayerRuleTests.cs`):

- `Domain` → anything
- `Business` → `Infrastructure`, EF Core, any DB driver, `HttpClient`, any portal type
- `Api` → excise-specific implementation, portal client, `DbContext`
- Any layer → `Microsoft.EntityFrameworkCore` outside `Infrastructure`

**Note on the Business↔Infrastructure boundary:** an excise provider lives in Business and needs a portal call. It **MUST** depend on an interface declared in Business (`IRjPortalClient`), implemented in `Infrastructure/Portal/RJ/`. Never a direct reference.

### Use only the layers the operation needs

| Operation | Pipeline |
|---|---|
| Health, static lookup | `Controller → Response` |
| Simple read, no business rule | `Controller → Query (Infrastructure) → DB` |
| Anything with a rule, a write, or excise variation | `Controller → Business → Domain → Infrastructure` |

A read that only filters and pages **MAY** skip Business. A write **MUST NOT**.

---

## 3. Solution structure

This is the **real** solution as it exists today, not a target sketch. Everything is .NET 8 except the React frontend, which is an `.esproj` (Vite + TypeScript) inside `src/` and part of the `.sln`.

```
C:\Projects\Liquor_Application\              repository root
├─ ST.LiquorTNT.sln                          14 projects (13 .NET + 1 .esproj), solution folders "src" and "tests"
├─ Directory.Build.props                     net8.0, nullable, ImplicitUsings, TreatWarningsAsErrors (Release only)
├─ Directory.Packages.props                  central package versions — ⛔ Version= on a PackageReference
├─ README.md                                 run instructions
├─ db/mysql/                                 001_schema.sql, 002_seed_admin.sql (hand-run today; migrations later)
├─ src/
│  ├─ ST.LiquorTNT.Api/                      Web API host
│  ├─ ST.LiquorTNT.Business/                 use cases, validators, interfaces it needs
│  ├─ ST.LiquorTNT.Domain/                   entities and rules — references nothing
│  ├─ ST.LiquorTNT.Contracts/                request/response types — references nothing
│  ├─ ST.LiquorTNT.Infrastructure/           EF Core, MySQL/SQL Server, identity, (later) portals, files, outbox
│  ├─ ST.LiquorTNT.Logging/                  Serilog host setup, JSON formatter, log-mode switch, secret masking
│  ├─ ST.LiquorTNT.Desktop/                  WinForms line application (net8.0-windows) — references Contracts only
│  └─ ST.LiquorTNT.WebApplication/           React (Vite + TypeScript) — `.esproj`, in the .sln
└─ tests/
   ├─ ST.LiquorTNT.Domain.Tests/             entity rules, no DB
   ├─ ST.LiquorTNT.Business.Tests/           services with fakes, no DB
   ├─ ST.LiquorTNT.Infrastructure.Tests/     hashing today; EF/SP/view tests on both providers later (Testcontainers)
   ├─ ST.LiquorTNT.Api.Tests/                WebApplicationFactory HTTP tests (placeholder today)
   ├─ ST.LiquorTNT.Edge.Tests/               line application tests (net8.0-windows, placeholder today)
   └─ ST.LiquorTNT.Architecture.Tests/       NetArchTest — the rules in §2, fails the build
```

**Not created on purpose:** `ST.LiquorTNT.Integration` (+ its test project) — deferred until portal work starts. `ST.LiquorTNT.Shared` — dropped; there is nothing for it to hold that `Contracts` or `Business/Common` does not already hold.

### Api

```
ST.LiquorTNT.Api/
├─ Controllers/          Auth, Users, Roles, Pages, SupplierCodes, LiquorCategories today. Grows to: Company, Brand, Batch, Plan,
│                        CodePool, Palette, Case, Aggregation, Dispatch, PortalSync, Outbox, Report,
│                        Setting, Supplier code, ScreenConfig; Line/ for the line-app endpoints
├─ Middleware/           CorrelationMiddleware.cs → ExceptionMiddleware.cs  (RequestLogging later)
├─ Security/             CurrentUser.cs, TenantContext.cs — read claims from the JWT, nothing else
├─ Extensions/           ApiServiceExtensions.cs — the ONLY file that references Infrastructure
├─ Properties/launchSettings.json
├─ Logging/              MethodLoggingProxy (DETAIL: Business method input/output), LogModeRefresher (LOG_MODE from DB)
├─ Program.cs            pipeline: correlation → request logging → exception → swagger(dev) → cors → auth → session → controllers → /health
└─ appsettings.json      ConnectionStrings:Default, Database:{Provider,ServerVersion}, Jwt, Cors, Serilog (sinks + JSON formatter; levels come from LOG_MODE)
```

### Business — organised by feature, module names identical to the frontend features

```
ST.LiquorTNT.Business/
├─ Auth/                 IAuthService, AuthService, LoginRequestValidator, AuthOptions,
│                        IUserRepository, IAccessTokenService   ← interfaces live with the feature that owns them
├─ Common/
│  ├─ ITenantContext.cs  ICurrentUser.cs  IClock.cs  ErrorCodes.cs
│  ├─ Exceptions/        AppException (base), ValidationException, UnauthorizedException,
│  │                     ForbiddenException, NotFoundException, BusinessException
│  └─ Abstractions/      ONLY cross-module interfaces that Infrastructure implements (IPasswordHasher today)
├─ DependencyInjection.cs   AddBusiness()
└─ (next) Brands/ Batches/ Companies/ Plans/ CodePool/ Palette/ Production/ Aggregation/
          CaseData/ Dispatch/ PortalSync/ Outbox/ Reports/ Settings/ Supplier code/ ScreenConfig/ FeatureFlags/ Line/
          Excise/  IBrandProvider ICaseCodeGenerator IBottleCodeValidator IPortalProcessor
                   IDispatchProcessor IExciseRules IExciseCapabilityResolver
                   RJ/ JK/ MP/ UP/ DL/ CG/ AS/ HP/ TN/ KL/ MG/  Common/
```

### Domain

```
ST.LiquorTNT.Domain/
├─ Entities/             USERS, ROLES, ROLE_RIGHTS, USER_ROLES, USER_RIGHTS, PAGES, PAGE_ACTIONS, SUPPLIER_CODE, LIQUOR_CATEGORY … today; then Brand, Batch, Bottle,
│                        Case, BottleMapping, Dispatch, Palette, OutboxMessage, AuditEntry
├─ Exceptions/           DomainException.cs
└─ (next) ValueObjects/  CaseCode, HologramNumber, BrandCode, Gtin, SerialRange, ExciseCode
          Rules/  Events/
```

### Infrastructure

```
ST.LiquorTNT.Infrastructure/
├─ Database/
│  ├─ AppDbContext.cs                  the only DbContext
│  ├─ Configurations/                  UserConfiguration.cs, UserRightConfiguration.cs (IEntityTypeConfiguration<T>)
│  ├─ Repositories/                    UserRepository.cs
│  └─ (next) Interceptors/  Queries/MySql/  Queries/SqlServer/
│             Scripts/MySql/{StoredProcedures,Views,Triggers,Partitioning}
│             Scripts/SqlServer/{StoredProcedures,Views,Triggers,Partitioning}
│             Migrations/MySql/  Migrations/SqlServer/  Bulk/
├─ Identity/             Pbkdf2PasswordHasher.cs, JwtAccessTokenService.cs, JwtOptions.cs
├─ Common/               SystemClock.cs
├─ DependencyInjection.cs   AddInfrastructure() — holds the ONLY provider switch (MySql / SqlServer)
└─ (next) Portal/<State>/  Files/  Outbox/  BackgroundWorkers/  Secrets/
```

### Logging

```
ST.LiquorTNT.Logging/
├─ LoggingSetup.cs       UseAppLogging(): Serilog sinks from configuration, levels bound to LogModeSwitch
├─ LogModeSwitch.cs      NORMAL / DETAIL, switched live
├─ AppJsonFormatter.cs   one JSON object per line, IST time, readable message
├─ SafeJson.cs           serialise + mask secrets (password, answer, token, hash …) before anything is logged
└─ LogFields.cs          property names every entry carries (CorrelationId, UserId, CompanyId, SupplierCodeId, ExciseCode, …)
```

Host logging only. The audit / user log is a business feature and lives in Infrastructure, not here.

### Contracts — the wire format consumed by React and the line app (source of the OpenAPI document)

```
ST.LiquorTNT.Contracts/
├─ Auth/                 LoginRequest, LoginResponse, CurrentUserResponse
├─ Common/               PagedResponse.cs
└─ (next) Brands/ Batches/ Cases/ Dispatch/ Reports/ Line/ …
```

`Business` **MAY** return Contracts types directly — we deliberately do not add a second mapping layer. The consequence is accepted: a contract change touches Business.

### Desktop — the line application

`ST.LiquorTNT.Desktop` is the WinForms program that runs on the bottling line (camera, printer, PLC). It is in this solution so that it shares `Contracts` and the build, and **nothing else**: it references only `Contracts` and reaches the server through `/api/line/...`. ⛔ A `DbContext`, a connection string or a Business type in Desktop.

---

## 4. Naming conventions

| Item | Convention | Example |
|---|---|---|
| Project | `ST.LiquorTNT.<Layer>` | `ST.LiquorTNT.Business` |
| Namespace | matches folder path | `ST.LiquorTNT.Business.Brands` |
| **Domain entity** | **exactly the DB table name (verbatim, CAPITAL)** — so entity↔table is obvious. Properties stay PascalCase, mapped to columns in the `IEntityTypeConfiguration`. | table `USERS` → `USERS`; `SECURITY_CONFIG` → `SECURITY_CONFIG` |
| Controller | `<Noun>Controller` | `BrandController` |
| Business class / its interface | `<Feature>Service` / `I<Feature>Service`, same folder | `BrandService`, `IBrandService` |
| Interface | `I<Name>` | `IBrandProvider` |
| Excise implementation | `<Code><Capability>` | `RjBrandProvider`, `MpCaseCodeGenerator` |
| Request / Response | `<Verb><Noun>Request`, `<Noun>Response` | `CreateBrandRequest`, `BrandResponse` |
| Query (Infrastructure) | `<Name>Query` + provider prefix | `MySqlProductionReportQuery` |
| SP script file | `<Operation>.sql`, same name in both providers | `GetProductionReport.sql` |
| SP name in DB | `sp_<Operation>` | `sp_GetProductionReport` |
| View | `vw_<Name>` | `vw_CaseSummary` |
| Route | all lowercase, words joined — **no `-`**, plural, no version segment ([ADR 0001](adr/0001-response-bodies-and-routes.md)) | `/api/brands`, `/api/line/casecodes`, `/api/auth/changepassword` |
| Permission key | `<module>.<action>` — **identical to the frontend list** | `brand.insert`, `dispatch.send` |
| Feature flag key | camelCase — identical to the frontend list | `dispatch`, `codePool` |
| Async method | `...Async` | `GetBrandsAsync` |
| Test | `Method_Scenario_Expectation` | `Allocate_WhenTwoLinesRequest_ReturnsDistinctBlocks` |

Excise codes are the fixed set: `UP RJ MP JK DL CG AS HP TN KL MG`.

---

## 5. API layer rules

**Controller shape — controllers are thin, no exceptions.**

```csharp
[ApiController]
[Route("api/brands")]
public sealed class BrandController : ControllerBase
{
    private readonly IBrandService _brands;
    public BrandController(IBrandService brands) => _brands = brands;

    [HttpGet]
    [HasPermission("brand.view")]
    public async Task<ActionResult<PagedResponse<BrandResponse>>> GetAsync(
        [FromQuery] GetBrandsRequest request, CancellationToken ct)
        => Ok(await _brands.GetAsync(request, ct));

    [HttpPost]
    [HasPermission("brand.insert")]
    public async Task<ActionResult<BrandResponse>> CreateAsync(
        CreateBrandRequest request, CancellationToken ct)
        => Ok(await _brands.CreateAsync(request, ct));
}
```

Rules:

- Controllers **MUST NOT** contain business rules, SQL, portal calls, excise conditions, or `try/catch`.
- Every endpoint **MUST** carry `[HasPermission("...")]` unless it is part of the auth flow or `/health`.
- Every async endpoint **MUST** accept and pass `CancellationToken`.
- Request validation uses FluentValidation, registered in the pipeline; a controller never validates manually.
- `CompanyId`, `SupplierCodeId`, `ExciseCode` **MUST NOT** be accepted as request parameters. They come from `ITenantContext`. ⛔ (Only exception: Admin naming the company a new supplier code or first user belongs to - that is the data being created, not the caller's scope.)

**Response format**

- Success: the payload itself, with the proper status code — `200` read/update/action, `201` + `Location` create. **Every response has a body; no `204`** ([ADR 0001](adr/0001-response-bodies-and-routes.md)). An action on a record (activate, unlock …) returns the record after the change; an action with no record (logout, verify, delete) returns `MessageResponse` → `{ "message": "Logged out." }`.
- List: `PagedResponse<T>` → `{ "items": [], "page": 1, "pageSize": 50, "totalCount": 500 }`.
- Error: **RFC 7807 ProblemDetails**, produced only by `ExceptionMiddleware`:

```json
{
  "type": "https://errors.stliquortnt.local/brand_duplicate",
  "status": 409,
  "errorCode": "BRAND_DUPLICATE",
  "title": "Brand already exists",
  "detail": "A brand with ETIN 1234567 already exists for this company.",
  "instance": "/api/brands",
  "correlationId": "6f1c8e2a-...",
  "errors": { "etin": ["Already used"] }
}
```

The same seven keys on every error, in this order:
- `type`: an RFC 7807 problem-type URI. Today it is `errorCode` as a URL; its real target (for example an error page in the API guide) is still to be decided, so it is kept;
- `status`;
- `errorCode`: stable; the client branches on it, never on the text;
- `title`: short, shown to the user;
- `detail`: what to do next, or `null`;
- `instance`: the path that failed;
- `correlationId`.

`errors` is added for 400. `exceptionType` and `stackTrace` are added for 500.

`errors` is present only for validation failures (400) and maps field → messages so the frontend can place them on the form.

**Status code mapping** — `ValidationException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `BusinessException` 409, `IntegrationException` 502, `DbException` 500 (`DATABASE_ERROR`), anything else 500 (`UNEXPECTED_ERROR`).

**Failures must be diagnosable from the response.** A 500 that says only "an unexpected error occurred" wastes a developer's day. `ExceptionMiddleware` therefore:

- unwraps the **inner-exception chain** and puts it in `detail` (`TargetInvocationException: ... --> MySqlException: Table 'x' doesn't exist`) — the real cause is almost always the innermost exception;
- catches `DbException` separately so a database failure is labelled `DATABASE_ERROR`, not lumped into "unexpected";
- adds `exceptionType` and the first 15 stack frames — **in every environment**, Production included;
- gives a ProblemDetails body (with `errorCode`) to the errors the framework answers on its own: no route `ENDPOINT_NOT_FOUND` 404, wrong method `METHOD_NOT_ALLOWED` 405, body not JSON `UNSUPPORTED_MEDIA_TYPE` 415, role refused `FORBIDDEN` 403. MVC's own client-error mapping is switched off (`SuppressMapClientErrors`) so there is one error shape.

Showing exception text in Production is an **owner decision** ([ADR 0001](adr/0001-response-bodies-and-routes.md)): the system is on-premise and support works from the response. The accepted cost is that table names, SQL fragments and file paths reach the client, so the API must stay on the plant network. The `correlationId` still links every response to the log entry.

**Versioning** — routes carry no version segment. One build runs at a plant, so the server and the line apps always upgrade together; the safety net is the build check in §11, not a URL version.

---

## 6. Business layer rules

- One class per feature: `BrandService`, `DispatchService`. Split when a file exceeds ~400 lines.
- A business method **MUST** read tenant context from `ITenantContext`, never from the request.
- **Interfaces — exactly two kinds are allowed:**
  1. `I<Feature>Service` for every module (`IAuthService`, `IBrandService`), declared next to its implementation in the same feature folder. A convention, kept so every module has the same shape and API tests can inject a fake.
  2. An interface whose implementation lives in **another project** — repositories, token/hashing services, provider-specific queries, the excise capabilities. Mandatory, because Business may not reference Infrastructure.
- **Placement:** an interface belongs to the feature that owns it (`Business/Auth/IUserRepository.cs`, `Business/Auth/IAccessTokenService.cs`). `Common/` holds only what three or more modules use (`IClock`, `ITenantContext`, `ICurrentUser`); `Common/Abstractions/` holds only such cross-module interfaces that Infrastructure implements (`IPasswordHasher`). ⛔ `Common/Abstractions` as a dumping ground for every module's interfaces — this mistake was made once and reverted.
- **Domain declares no interfaces.** A Domain method that needs a limit or a rule takes it as a plain value (`user.RegisterFailedLogin(3)`), never as an injected service. This keeps Domain free of abstractions and of any dependency direction question.
- Excise variation **MUST** be resolved through a capability, never through a condition:

```csharp
// ⛔ forbidden anywhere outside Business/Excise/*
if (tenant.ExciseCode == "RJ") { ... }

// ✅ required
var provider = _capabilities.Resolve<IBrandProvider>(tenant.ExciseCode);
var brands   = await provider.GetBrandsAsync(filter, ct);
```

- `IExciseCapabilityResolver` resolves by excise code from a registry built at startup. If a state does not implement a capability, resolution **MUST** fail fast at startup, not at the first request.
- Capability interfaces are **narrow and per-behaviour** (`IBrandProvider`, `ICaseCodeGenerator`, `IBottleCodeValidator`, `IPortalProcessor`, `IDispatchProcessor`, `IExciseRules`). ⛔ One large `IExciseService`.
- Business **MUST NOT** contain SQL, SP names, EF types, `HttpClient`, or file paths.

**Code vs configuration — mandatory split**

| Goes in code (Business/Domain/Excise) | Goes in database configuration |
|---|---|
| RJ 28-digit case code composition | JK carton height max = 350, MP = 400 |
| MP hologram range validation logic | Retry count, portal timeout |
| "A bottle cannot be aggregated twice" | Which modules are enabled for a customer |
| "A dispatched case cannot be modified" | Field labels, visible columns, screen config |
| Dispatch state transitions | FL/CL applicability, pack-size options |

⛔ Executable logic stored as strings in the database.

---

## 7. Domain layer rules

- Entities hold their own invariants; setters are `private`. State changes happen through methods (`case.MarkDispatched()`), not property assignment from outside.
- Core rules that are true for every state live here: unique bottle code, a case belongs to a valid batch, a dispatched case is immutable, an aggregation cannot repeat a bottle.
- Value objects for identifiers that have a format (`CaseCode`, `HologramNumber`, `Gtin`) — validation in the constructor.
- ⛔ EF attributes, `DbContext`, HTTP, file, portal, or provider-specific types in Domain. EF mapping lives in `Infrastructure/Database/Configurations`.

---

## 8. Infrastructure rules

### 8.1 EF Core

- `AppDbContext` is the only `DbContext`. Mapping through `IEntityTypeConfiguration<T>` classes, never attributes.
- Interceptors handle, automatically: audit columns (`CreatedBy/At`, `ModifiedBy/At`), tenant filter, soft delete filter, outbox row creation. Developers do not write these by hand.
- Reads that do not mutate use `AsNoTracking()`.
- Every list query **MUST** project to a response type (`Select`) — never return entities to the API.
- ⛔ `ToList()` before filtering; ⛔ unbounded queries without paging.

### 8.2 Stored procedures, views, triggers

Stored procedures, views and triggers are **first-class tools** here and are used wherever the use case calls for them — reporting, heavy aggregation, batch operations, complex joins, provider-specific optimisation. There is no restriction on when to use them. What is fixed is **where they live, how they are shipped, and how they are verified**:

1. **Placement** — all SQL objects live under `Infrastructure/Database/Scripts/<Provider>/`. Business and Api **MUST NOT** contain SQL text or SP names. A SP is invoked through an interface declared in Business and implemented per provider:

```csharp
// Business/Reports/IProductionReportQuery.cs   ← owned by the Reports feature, not Common/
public interface IProductionReportQuery
{
    Task<IReadOnlyList<ProductionReportRow>> RunAsync(ProductionReportFilter f, CancellationToken ct);
}
// Infrastructure/Database/Queries/MySql/MySqlProductionReportQuery.cs      → sp_GetProductionReport (MySQL)
// Infrastructure/Database/Queries/SqlServer/SqlServerProductionReportQuery.cs → sp_GetProductionReport (SQL Server)
```

2. **Versioning** — scripts are committed with the application and applied by migration. ⛔ Applying a SP manually on a customer database. Every script is idempotent (`CREATE OR REPLACE` / drop-and-create) and carries a header comment: purpose, owner, date, why a SP was chosen.
3. **Parity** — the file set under `Scripts/MySql/` and `Scripts/SqlServer/` **MUST** match. CI fails when an object exists on one side only.
4. **Testing** — every SP or view that produces a business result **MUST** have an integration test, and that test runs against **both** providers.
5. **Tenant safety** — global query filters do **not** apply to SPs, views or raw SQL. Every such object **MUST** take `CompanyId` / `SupplierCodeId` / `ExciseCode` as parameters, supplied from `ITenantContext`. ⛔ Passing a tenant value that came from the HTTP request. This is a security rule, not a style rule.
6. **Invocation differs per provider even when the SP is identical** — MySQL uses `CALL sp_Name(@p0, @p1)`, SQL Server uses `EXEC sp_Name @p0, @p1`. This is why an SP is always reached through two small provider-specific caller classes behind one interface, never one class with an `if`.
7. **LINQ cannot be composed over a stored procedure.** `FromSqlRaw("CALL ...").Where(...).Skip(...)` does not work — EF cannot wrap an SP call. Every filter, sort and page must become an SP parameter and be applied inside the SP.
8. **Views are the preferred shape for read models and reports**, and are the right answer whenever the caller still needs to filter, sort or page. A view is mapped as an EF keyless entity (`HasNoKey().ToView("vw_Name")`), after which normal LINQ composes over it — so the SQL is written once per provider and **the C# stays identical on both**. Reach for an SP only when the work is genuinely procedural (multiple steps, loops, bulk operations, maintenance).
9. **Triggers** — allowed. Two things to know before adding one: on SQL Server, a table with a trigger must be declared with `.ToTable(t => t.HasTrigger("name"))` or EF Core saves fail (EF uses an `OUTPUT` clause, which SQL Server disallows on trigger tables); and on the bottle/case hot tables a row-level trigger is a throughput cost at 4.8M rows/day. Prefer triggers for master-table auditing, and the application path for hot tables.

### 8.3 Dual provider (MySQL primary, SQL Server supported)

- Provider is chosen by configuration:

```json
{
  "ConnectionStrings": { "Default": "Server=...;Port=3306;Database=st_tnt_liquor;User ID=...;Password=...;" },
  "Database": { "Provider": "MySql", "ServerVersion": "8.0.36" }
}
```

`Database:ServerVersion` is declared explicitly because `ServerVersion.AutoDetect` would open a connection during startup. The switch on `Database:Provider` exists in exactly one place — `Infrastructure/DependencyInjection.cs`. The `sqlserver` branch throws `NotSupportedException` until the SQL Server provider package is added; the branch is kept so the decision is visible in code.

- **MySQL is primary.** Schema, migrations and tuning are designed on MySQL first; SQL Server follows. ⛔ Using a SQL Server feature that has no MySQL equivalent in the shared schema (e.g. `SEQUENCE` objects).
- Logical schema is **identical** on both providers. Physical partitioning is provider-specific DDL under `Scripts/<Provider>/Partitioning/`.
- Hot transaction tables carry no foreign keys on either provider (MySQL does not allow FK on partitioned InnoDB tables; we keep both sides the same).
- **Explicitly set, never inherit the default:**
  - **Collation** on all code/identifier columns (case code, hologram, brand code). A duplicate check that behaves differently per provider is a data defect, not a performance detail.
  - **Transaction isolation level** — MySQL InnoDB defaults to `REPEATABLE READ`, SQL Server to `READ COMMITTED`.
- **Error codes are mapped, not compared raw.** MySQL `1062` duplicate key / `1213` deadlock / `1205` lock-wait-timeout; SQL Server `2627`+`2601` duplicate key / `1205` deadlock. Note `1205` means different things on the two providers. Mapping lives in one place and drives retry behaviour.
- **Bulk write** has two implementations behind `IBulkWriter` — `MySqlBulkCopy`/`LOAD DATA` and `SqlBulkCopy`. This is the hot path; it is designed and measured, not incidental.
- **A feature is not done until both providers are done, in the same PR.**
- ⛔ Different business behaviour per provider. Only *how* data is fetched or written may differ; the result must be identical.

---

## 9. Multi-tenancy and security

Hierarchy: **Company → Excise → Supplier Code**. There is no plant entity.

`ITenantContext` carries `CompanyId, SupplierCodeId, ExciseCode`. `SessionValidationMiddleware` reads the supplier code the user picked
(`USER_SESSION.ACTIVE_SUPPLIER_CODE_ID`) and hands it over as `Api/Security/SessionScope`; without a picked supplier code, `CompanyId` is the
user's home company from the `company_id` claim.

- ⛔ Accepting any tenant value from the request body, query string or a client-supplied header.
- Every tenant-scoped entity implements `ITenantScoped`; the EF interceptor applies the filter globally.
- SPs, views and raw SQL take tenant parameters explicitly (§8.2 rule 5).
- Authorization is **server-side only**. The frontend hiding a button is not authorization. Permission keys match the frontend list exactly.

### 9.1 Roles and rights

Full design and API: `06-roles-rights-plan.md`. The rules every module follows:

- **Model (ERPNext-style):** a user holds several **master roles**, each for one supplier code or for all supplier codes of the company
  (`USER_ROLES.SUPPLIER_CODE_ID` null), plus **custom rights** (`USER_RIGHTS`) that only ever *add*. Effective rights in the active supplier code
  = union of both. Roles are company-wise; **Admin** (`ROLES.IS_SYSTEM`, the `admin` user; Sundaram Technologies' Super Admin is a separate, later feature) has every right in every company.
- **Permission key** = `PAGE_ACTIONS.PERMISSION_KEY` (`<page>.<action>`), seeded by SQL. Every key the backend checks is a constant in
  `Business/Access/Permissions.cs`; add the constant and the seed row together.
- **Every endpoint** carries `[HasPermission(Permissions.X)]` (`Api/Security/HasPermissionAttribute.cs`) except the auth flow and
  `/health`. The policy is built on the fly (`PermissionPolicyProvider`) and checked by `PermissionHandler` through
  `Business/Access/CurrentAccess`, which services also use for their own rules. Refusals: `409 SUPPLIER_CODE_NOT_SELECTED` (no supplier code picked),
  `403 PERMISSION_DENIED` (key missing; the detail names it).
- **Rights are read per request** (one query, kept for the request by the scoped `CurrentAccess`), never put in the JWT — a change applies
  on the next call.
- **Grant scope** (`PAGE_ACTIONS.GRANT_SCOPE`): `ANY`; `ADMIN` — only a holder of `user.manageadmin` may grant it; `SYSTEM` — never
  granted, Admin only (CRM masters). Admin users (holders of an `IS_ADMIN_ROLE` role, Plant Admin) are managed only by a holder of
  `user.manageadmin`; nobody but Admin changes their own roles or rights.
- **Password policy** with several roles = the strictest value of each rule (`PASSWORD_POLICY.Strictest`).

### 9.2 Status values and lookup tables

- A value the code branches on (e.g. `PAGE_ACTIONS.GRANT_SCOPE`, later approval statuses) is a C# `enum` stored as **text** in its
  column — no table. A business category that grows as data (`LIQUOR_CATEGORY`) gets its **own lookup table** with a foreign key.
  ⛔ A generic "LOOKUP" table.
- Page-specific steps (approve, reject, cancel …) are just more `PAGE_ACTIONS` rows of that page; who may take them is only ever a
  role right, never code. The batch approval design is recorded in `06-roles-rights-plan.md` §9 and is built with the batch page.

---

## 10. Excise-specific design, screen config and feature flags

**One common API for every state.** ⛔ `RjBrandController`, `/api/rj/brands`. One `BrandController`, one route, behaviour resolved by capability.

**Portal models are state-specific and stay inside their own folder.** `RjBrandResponse` never reaches Business or Contracts — a mapper converts it to the common model inside `Infrastructure/Portal/RJ/Mappers/`.

**Data source per state is configuration, not a code branch.** RJ brands may come from the local database or from the eConnect portal; the provider reads that from configuration. React never knows the source.

**Screen config** (`Business/ScreenConfig/`) serves the frontend's config-driven UI:

```
GET /api/screen-config/{screen}
→ { fields[], columns[], actions[], labels{}, mode }
   mode: manual | synced-readonly | synced-editable
```

Cached, invalidated on change. A missing config **MUST** return a valid default rather than an error, so a screen still renders.

**Feature flags** (`Business/FeatureFlags/`) replace the vendor-generated `setcompany.stx` file. Flags are stored per installation / excise / company, and both routes and modules respect them. Keys match the frontend `FeatureKey` union.

---

## 11. Line API

Same host, separate area, separate auth scheme, separate Swagger document, separate rate-limit bucket. It reuses the same Business layer.

| Concern | Rule |
|---|---|
| Route | `/api/line/...` |
| Auth | Machine registration key + line-user numeric PIN. Not the web cookie/JWT scheme. |
| Build check | Every request sends the line app build number. Mismatch with the server's expected build → `409` with a clear message. A mismatched line stops loudly instead of behaving wrongly. |
| Idempotency | Every write carries an `Idempotency-Key`. A repeated key returns the original result and writes nothing. Mandatory — a retried aggregation must not double-insert. |
| Code allocation | **Block allocation.** A line requests N codes (default 500); the server allocates the block atomically from a counter table (`UPDATE ... WHERE` row lock) and returns the range. The line consumes it locally. Provider-neutral by design — ⛔ SQL Server `SEQUENCE`. |
| Aggregation write | Bulk endpoint, `IBulkWriter` path, batched. Not one call per bottle. |
| Soft delete | Aggregation delete is soft. **A deleted aggregation still appears in the Case Report** (MP requirement) with its deleted state, and the audit entry is retained. |
| Heartbeat | Lines report machine, line number, build, last activity. Used by the diagnostics screen. |
| Offline resync | The line may replay unsent work; idempotency keys make replay safe. |

---

## 12. Outbox and background workers

Production **MUST** complete against the local database before any portal communication. ⛔ A production endpoint awaiting a portal response.

```
Production write → local DB (+ outbox row, same transaction) → worker → portal
```

- `OutboxMessage` statuses: `Pending → Processing → Completed | Failed → Retry`.
- Stored per message: excise, operation, payload, attempt count, next attempt time, last error, portal request/response reference, correlation id.
- Retry uses exponential backoff with a cap; after N attempts the message moves to `Failed` and appears on the diagnostics screen. ⛔ Silent give-up.
- Workers run as hosted services in the API host (one installation, on-premise). They **MUST** use their own connection pool and be throttled so archival cannot starve the API. A configuration switch allows running them as a separate process if a large site needs it.
- Jobs: outbox push, portal master sync, archival/partition maintenance, cleanup.

---

## 13. High-volume data rules

Design targets: ~100,000 cases/day, ~4,800,000 bottles/day, 1.5 billion+ rows/year.

- ⛔ Loading a transaction table into memory. Always filter and aggregate in the database.
- All list endpoints are server-paged; default page size 50, max 200.
- Always project; never materialise entities for reads.
- Writes from the line are batched through `IBulkWriter`.
- Large transaction tables are partitioned by time (month) and archived on a schedule; partitioning DDL is provider-specific.
- Reporting over wide date ranges goes through a SP or view, returning only the aggregated result.
- Indexes are designed with the query, in the same PR; a new query with no index plan is incomplete.

> **Open decision (ADR pending):** physical layout of the large transaction tables — excise-prefixed tables vs a single table with `excise_id` plus provider-specific partitioning. Current recommendation is the single logical table, so that EF mapping stays normal and the schema is identical on both providers. This must be settled before bulk development on the transaction path.

---

## 14. Logging

Three **separate** streams. They are not the same thing and must not be merged.

| Stream | Destination | Retention | Purpose |
|---|---|---|---|
| **Application log** | Structured file / Seq | 15–30 days | Diagnostics for developers and support |
| **Audit / User Log** | Database table, exposed as a screen with CSV export | Years (statutory) | Excise compliance, ISO, customer-visible |
| **Integration log** | Database, linked to the outbox message | Months | Portal disputes and failure analysis |

### Normal mode (default)

`Information` level. One entry per API call: correlation id, user, company, supplier code, excise, operation, duration, outcome. Business events are logged as summaries (`cases generated: 240`, `dispatch sent`, `sync completed`) — ⛔ one entry per bottle or per case.

### Detail mode

`Debug`/`Verbose`. Adds request/response payloads (masked), SQL and parameters with execution time, full portal request/response, capability-resolution trace (which provider was chosen and why), retry attempts.

Detail mode **MUST** obey all four:

1. **Runtime toggle**, no restart — support can ask a customer to enable it.
2. **Scoped** — by module, company and line. ⛔ Enabling verbose logging globally.
3. **Auto-expiry mandatory** — always enabled with a duration (30 min / 1 h / 4 h) and it switches itself off. At this volume an accidentally-left-on verbose switch fills the customer's disk in hours.
4. **Sampling on hot paths** — on line aggregation writes, log every Nth request plus all failures, never every request.

Every log entry automatically carries: timestamp, correlation id, user, company, supplier code, excise, operation, duration, outcome. Developers do not pass these by hand.

⛔ Never logged: passwords, PINs, tokens, portal credentials, connection strings, full personal data. Log ids and codes.

### Implemented today (2026-09-28)

- **Entry shape (same keys every time):** `Timestamp` (plain IST `yyyy-MM-dd HH:mm:ss.fff`), `Level` (Info / Warning / Error / Fatal), `CorrelationId` (`null` outside an API call), `Method`, `Message`, `Context`, and `Exception` only on errors.
  - `Method` is who wrote the entry: `AuthController.LoginAsync`, `AuthService.LoginAsync`, `SQL`, `Startup`, or else the logging class.
  - `Context` holds the details that are not in the message, or `null`: `ipAddress`, `userId`, and in DETAIL `request` / `response` / `input` / `output` as JSON objects, and `sql` as a list of lines.
  - `Exception` holds `ExceptionType`, `ExceptionMessage`, `InnerExceptionType`, `InnerException` (the innermost cause) and 20 `StackTrace` lines.
  - The output is indented JSON, readable like a Postman response, written by `Logging/AppJsonFormatter`. Framework plumbing (`RequestId`, `ConnectionId`, `SourceContext` …) and empty values are left out.
- **Files:** `logs/stliquortnt-YYYYMMDD.json` and the console. Files roll daily and at 100 MB, and 30 files are kept, so disk use is bounded. `logs/` is excluded from build and publish output. On a server, set a full path in `appsettings.json`.
- **Start-up is logged:**
  - Two-stage Serilog, so even a failure while the host is being built reaches the file.
  - `API starting` gives version, environment, machine, and DB provider / server / name (never the connection string), from `Api/Logging/StartupFacts`.
  - Then the hosting "Now listening on …" lines, and `Database reachable` or `Database check failed` from the first `LOG_MODE` read.
  - If the host throws: `Fatal` `API failed to start: …`, and the exception is re-thrown so IIS / the service manager still sees the failure.
- **What is logged:** API calls only (`/api/...`). Swagger, `/health` and static files are not logged.
  - Each call has one entry from `RequestLoggingMiddleware`, and a refusal carries its `errorCode` in the message.
  - `ExceptionMiddleware` logs only 500s, with the exception.
  - ASP.NET Core and EF report warnings and errors only; start-up messages are the exception.
- **Mode:** `SECURITY_CONFIG.LOG_MODE` = `NORMAL` | `DETAIL`. It is edited from the admin panel (`PUT /api/securityconfig/LOG_MODE`), and `Api/Logging/LogModeRefresher` applies it within 10 s without a restart.
  - NORMAL: the call entries, warnings and errors.
  - DETAIL also logs, per call:
    - the request and response bodies;
    - every Business service call via `Api/Logging/MethodLoggingProxy` — `OK` with input and output, or `Refused` / `Failed` with the input;
    - the SQL the call ran: EF `Executed DbCommand`, text only, never parameter values. SQL outside an API call is filtered out.
- **Masking:** `Logging/SafeJson` writes `***` for any property whose name ends in `password`, `pwd`, `pin`, `otp`, `token`, `hash`, `answer`, `secret` or `signingKey` (ignoring case and `_`). This covers bodies, inputs and outputs, in both modes.
  - A body that is not valid JSON is **not written at all** (`<body not logged: …>`), so a malformed login cannot leak a password.
  - `SafeJson` never throws.
  - Only services implemented in Business are wrapped: `IAccessTokenService` (Infrastructure, raw JWT output) never is.
- **Not yet implemented** from the rules above: scoping by module / company / line (today's switch is global), auto-expiry of DETAIL, and hot-path sampling (no line API yet). These wait for an owner decision.

**Correlation id** — generated by middleware per request (`X-Correlation-Id`), echoed in ProblemDetails, written to the application log, the audit entry and the outbox row. It is what lets support join a browser complaint to a database record.

---

## 15. Audit / User Log

A business feature, not application logging. Every state's existing application has this screen and it is required for compliance.

- Written for every user action that changes data, and for login/logout/lockout.
- Columns: user, company, supplier code, excise, module, screen, action, entity, entity id, timestamp, correlation id, before/after summary for updates.
- Written through `IUserLogWriter` (`Business/Common/Abstractions`): the service describes what happened (`UserLogEntry` — action type from `UserLogActions`, module, entity, plain before/after objects), the writer adds IP, user agent, correlation id and IST time and **stages** the row; the service's own `SaveChanges` commits the audit row and the change in one transaction. Create is the one exception (two commits, because the audit needs the generated id). An EF interceptor was considered and rejected: it cannot name the business action (login vs. lockout vs. reset) or the actor of an anonymous flow.
- Never contains a password, hash, security answer or token — callers pass response DTOs, not entities.
- Exposed through `/api/reports/user-log` with filters and CSV export.
- ⛔ Deleting or editing audit rows. Retention/archival only.

---

## 16. Error handling

Exception types in `Business/Common/Exceptions/`, all deriving from `AppException(errorCode, statusCode, title, detail?)`: `ValidationException` (carries `errors` per field), `UnauthorizedException`, `ForbiddenException`, `NotFoundException`, `BusinessException`. Added when their first use arrives: `IntegrationException`. Database failures are **not** wrapped in Business — `DbException` is caught by `ExceptionMiddleware` and reported as `DATABASE_ERROR`.

- `ExceptionMiddleware` is the **only** place exceptions become HTTP responses. ⛔ `try/catch` in controllers for the purpose of shaping a response.
- Every thrown exception carries a stable `errorCode` string (`BRAND_DUPLICATE`, `HOLOGRAM_OUT_OF_RANGE`) so the frontend can react without parsing messages.
- Error codes live in one place, `Business/Common/ErrorCodes.cs`, and are the contract the frontend reacts to. Infrastructure-level failures have codes too (`DATABASE_ERROR`, `UNEXPECTED_ERROR`).
- The response must name the real cause (inner-exception chain, exception type, stack frames) in every environment — see §5.
- Transient database errors (deadlock, lock-wait-timeout, connection reset) are retried inside Infrastructure with backoff, using the provider error-code mapping from §8.3. Business never sees them.
- Integration failures **MUST NOT** fail the production request; they become outbox retries.

---

## 17. Testing

| Project | Scope | Notes |
|---|---|---|
| `ST.LiquorTNT.Domain.Tests` | Entity rules (`UserTests` today) | Fast, no DB, no network |
| `ST.LiquorTNT.Business.Tests` | Services with fakes (`AuthServiceTests` + `Fakes.cs` today), validators, excise capabilities, capability resolution | Fast, no DB, no network |
| `ST.LiquorTNT.Infrastructure.Tests` | Hashing today; EF mappings, SPs, views, transactions, outbox, bulk writer, tenant isolation | Testcontainers; **runs on MySQL and SQL Server** |
| `ST.LiquorTNT.Api.Tests` | Full HTTP: auth, permissions, validation, response shape, status codes, tenant isolation, idempotency | `WebApplicationFactory` (add `Microsoft.AspNetCore.Mvc.Testing` + a reference to `ST.LiquorTNT.Api` when the first test is written) |
| `ST.LiquorTNT.Edge.Tests` | Line application (`ST.LiquorTNT.Desktop`) | `net8.0-windows` |
| `ST.LiquorTNT.Architecture.Tests` | The reference rules in §2 | NetArchTest; a violation fails the build |

Rules:

- xUnit is the framework. FluentAssertions for assertions.
- Business rules are tested by unit tests, not API tests. Stored procedures are tested by integration tests, not unit tests.
- Every bug fix ships with a test that fails before the fix.
- CI matrix runs the integration suite against **both** providers; a divergence fails the build, not a customer.
- Tenant isolation has at least one dedicated test per module: user of Company A **MUST NOT** see Company B data — including through SP-backed endpoints.

---

## 18. CI pipeline

`restore → build (warnings as errors) → unit tests → architecture tests → script parity check → integration tests (MySQL) → integration tests (SQL Server) → API tests → publish OpenAPI`.

The **script parity check** compares the file set under `Scripts/MySql/` and `Scripts/SqlServer/` and fails on any object present in only one.

The published OpenAPI document is the input for the frontend's generated client; a contract change and its frontend regeneration are linked PRs.

---

## 19. Checklist — adding a new module

1. Confirm the module name matches the frontend feature name.
2. Add permission keys and, if flagged, the feature-flag key — matching the frontend lists.
3. `Contracts/<Module>/` — request and response types.
4. `Domain/` — entity and rules, if the module owns a business object.
5. `Business/<Module>/` — `I<Module>Service` + `<Module>Service`, validators, options; declare any interface Infrastructure must implement **in this folder**, and register in `Business/DependencyInjection.cs`.
6. If behaviour varies by state — add a capability interface in `Business/Excise/` and implementations per state; ⛔ conditions in the service.
7. `Infrastructure/` — EF configuration, repositories/queries; SP/view scripts in **both** provider folders if used; register in `Infrastructure/DependencyInjection.cs`.
8. `Api/Controllers/<Module>Controller.cs` — thin, permission attribute on every endpoint.
9. Screen config entry if the frontend screen is config-driven.
10. Tests: unit for rules, integration for data access (both providers), API for the endpoints, tenant-isolation test.
11. `dotnet test` green locally, then PR.

---

## 20. Never do this

| ⛔ | Instead |
|---|---|
| `if (excise == "RJ")` outside `Business/Excise/*` | Capability interface + per-state implementation |
| A separate controller, route or API per state | One common API |
| Tenant values taken from the request | `ITenantContext` |
| SP, view or raw SQL without tenant parameters | Explicit tenant parameters from the context |
| SQL text or SP names in Business or Api | Interface in Business, implementation in Infrastructure |
| EF Core, DB driver or `HttpClient` types in Business or Domain | Interfaces declared in Business |
| Custom `{success, data, ...}` response envelope | Payload + status code; ProblemDetails for errors |
| Portal model types reaching Business or Contracts | Map inside the portal folder |
| Production waiting for a portal response | Local write + outbox |
| Different business behaviour per database provider | Same result; only the query may differ |
| A feature shipped for one provider only | Both providers, same PR |
| Executable logic stored as database strings | Logic in code, values in configuration |
| Loading a transaction table into memory | Filter, project and aggregate in the database |
| One log entry per bottle | Summary logging + sampling |
| Verbose logging enabled without an expiry | Scoped toggle with auto-expiry |
| Editing or deleting audit rows | Archive only |
| `try/catch` in controllers to shape a response | `ExceptionMiddleware` |
| A generic repository, or an interface invented for its own sake | Follow the interface rule in §6; anything beyond it waits for a second implementation |
| Microservices, event bus, separate DB per state | One modular monolith, one database |

---

## 21. Implementation status (2026-10-07)

| Done | Not yet |
|---|---|
| Solution scaffold, 13 projects, central package versions, 7 architecture rules | EF migrations (User-module schema is hand-run SQL `db/mysql/003`–`005`; the base `USERS`/`ROLES`/`COMPANY`/`USER_LOG` tables come from the legacy merge) |
| **User module, complete:** users CRUD + activate/deactivate/unlock, role-wise password policy + validation + history, login with IST-day lockout, server-side sessions (limit/expiry/logout/revoke), change password, security questions + forgot-password (wrong answers count towards the lock), `USER_LOG` audit in the same transaction, admin editing of `SECURITY_CONFIG` and `PASSWORD_POLICY`. Two independent audits applied. | Batch page (create / approve / cancel, design in docs/06 §9); rate limiting on anonymous auth routes; row lock for concurrent logins at the session limit |
| **Roles & rights module, complete (docs/06):** company-wise master roles + custom rights per supplier code, Admin, default role templates copied on a company's first supplier code, `[HasPermission]` on every endpoint, supplier code picked after login and kept in the session, admin-user protection (`user.manageadmin`), grant scopes ANY / ADMIN / SYSTEM, strictest password policy across roles, supplier code and liquor category masters. Script `db/mysql/009`. | Company and brand masters; SQL Server copy of `009` |
| PBKDF2 hashing; JWT issue + validation; SHA-256 session/reset token hashing | Tenant filter interceptor; detail-mode logging toggle; request logging middleware |
| ProblemDetails for every error path incl. model binding, `DATABASE_ERROR`, correlation header preserved | SQL Server provider package; Testcontainers (database tests run against the dev MySQL, overridable via `ST_TNT_TEST_CONNECTION`) |
| 422 tests: Domain 74, Business 215, Infrastructure 21, Api 104, Architecture 7, Edge 1 (2 cookie tests in Api fail: the test helper sends the URL-encoded XSRF cookie value; not caused by roles) | Every business module after Users/Auth; Line API; outbox; portals |

The former `tnt_user` scaffold was removed; the User module is built on the CAPITAL-named legacy tables (`USERS`, `ROLES`, `COMPANY`, `USER_LOG`) plus the tables added by `003`. Entity classes carry the exact table names (§4).

---

## 22. Open decisions (ADR pending)

1. **Transaction table layout** — excise-prefixed tables vs single table with `excise_id` + partitioning (§13). Blocks bulk work on the transaction path.
2. **Cross-excise combined reporting** — required or always per-state? Decides (1).
3. **Data retention** — how long transaction data stays live before archive.
4. **Peak concurrency** — how many lines run simultaneously at one plant; sizes the allocation block and connection pool.
5. **MP Dispatch and Task modules** — undocumented; CG's dispatch workflow is the reference shape.
6. **KL and MG** — no SOP received.

---

*Change proposals go to the backend lead. This is the single source of truth; the AI coding guide (`04-ai-coding-guide.md`) is a condensed copy and must be updated with it. Anything touching §2 reference rules, §5 response format, §8.3 dual-provider rules, or the capability interface set requires an ADR in `docs/adr/`.*