# Backend AI Coding Guide — Excise Track & Trace

> **How to use:** give this whole document to the AI before asking it to generate backend code (in VS Code with Claude, `CLAUDE.md` at the repo root already points here). Full detail is in `02-backend-architecture-standards.md`; the file-by-file map is `03-solution-structure.md`. Version 1.2 — 2026-09-25.

## 1. Context and stack

One **ASP.NET Core Web API (.NET 8)** replacing eleven state-wise desktop applications for liquor excise track & trace in India. One on-premise installation serves **multiple companies, supplier codes and excise states at once**. States: `UP RJ MP JK DL CG AS HP TN KL MG`.

About 80% of the behaviour is identical everywhere. The other 20% differs per state — case code format, source of master data, portal format — and is isolated into state components, never written as conditions in common code.

| | |
|---|---|
| Runtime | .NET 8, C# latest, `nullable enable`, warnings as errors |
| Database | **MySQL 8 primary**, **SQL Server supported**; one customer runs one provider, chosen by configuration |
| Data access | EF Core for normal work; stored procedures, views and triggers used freely where the use case calls for them |
| Validation / Tests / Logs | FluentValidation · xUnit + FluentAssertions + Testcontainers + NetArchTest · Serilog |
| Errors | RFC 7807 ProblemDetails |

## 2. Solution structure

```
C:\Projects\Liquor_Application\          repository root
├─ ST.LiquorTNT.sln
├─ Directory.Build.props / Directory.Packages.props   (central versions — never Version= on a PackageReference)
├─ db/mysql/                             001_schema.sql, 002_seed_admin.sql
├─ src/
│  ├─ ST.LiquorTNT.Api/             controllers, middleware, security (claims), Swagger, DI wiring
│  ├─ ST.LiquorTNT.Business/        use cases + validators + the interfaces they need + state components
│  ├─ ST.LiquorTNT.Domain/          entities, value objects, core rules — references nothing
│  ├─ ST.LiquorTNT.Contracts/       request/response types shared with React and the line app — references nothing
│  ├─ ST.LiquorTNT.Infrastructure/  EF Core, MySQL/SQL Server, SP/views, identity, portals, files, outbox
│  ├─ ST.LiquorTNT.Logging/         Serilog host setup only
│  ├─ ST.LiquorTNT.Desktop/         WinForms line application — references Contracts only
│  └─ ST.LiquorTNT.WebApplication/  React (`.esproj`, in the .sln)
└─ tests/  Domain.Tests · Business.Tests · Infrastructure.Tests · Api.Tests · Edge.Tests · Architecture.Tests
```

The business layer is named **`Business`** (not "Application"). `ST.LiquorTNT.Integration` and `ST.LiquorTNT.Shared` do **not** exist — do not create them.

**Api** — `Controllers/` (Auth, Users, Roles, Pages, SupplierCodes, LiquorCategories today; then Company, Brand, Batch, Plan, CodePool, Palette, Case, Aggregation, Dispatch, PortalSync, Outbox, Report, Setting, Supplier code, ScreenConfig) · `Controllers/Line/` (LineAuth, LineMaster, LineCode, LineAggregation, LineHeartbeat) · `Middleware/` (Correlation → Exception → RequestLogging) · `Security/` (CurrentUser, TenantContext from the session + claims, HasPermission + PermissionPolicyProvider/Handler) · `Extensions/ApiServiceExtensions.cs` (the only file referencing Infrastructure) · `Program.cs`

**Business** — one folder per feature, names matching the frontend features:

```
Auth/      IAuthService  AuthService  LoginRequestValidator  AuthOptions
           IUserRepository  IAccessTokenService        ← interfaces live in the feature that owns them
Access/ Roles/ Users/ SupplierCodes/ LiquorCategories/  (today)  Brands/ Batches/ Companies/ Plans/ CodePool/ Palette/
Production/ Aggregation/ CaseData/ Dispatch/ PortalSync/ Outbox/ Reports/
Settings/ Supplier code/ ScreenConfig/ FeatureFlags/ Line/
Excise/    IBrandProvider  ICaseCodeGenerator  IBottleCodeValidator
           IPortalProcessor  IDispatchProcessor  IExciseRules
           IExciseCapabilityResolver
           RJ/ JK/ MP/ UP/ DL/ CG/ AS/ HP/ TN/ KL/ MG/   ← state implementations
Common/    ITenantContext  ICurrentUser  IClock  ErrorCodes
           Exceptions/ (AppException, ValidationException, UnauthorizedException,
                        ForbiddenException, NotFoundException, BusinessException)
           Abstractions/ (ONLY cross-module interfaces Infrastructure implements — IPasswordHasher)
DependencyInjection.cs   AddBusiness()
```

**Domain** — `Entities/` (USERS, ROLES, USER_ROLES, SUPPLIER_CODE … today; then Brand, Batch, Bottle, Case, BottleMapping, Dispatch, Palette, OutboxMessage, AuditEntry) · `ValueObjects/` (CaseCode, HologramNumber, BrandCode, Gtin, SerialRange, ExciseCode) · `Rules/` (GrantScope) · `Events/` · `Exceptions/` (DomainException). Private setters, behaviour through methods, **no interfaces, no EF attributes**.

**Infrastructure**

```
Database/   AppDbContext  Configurations/  Repositories/  Interceptors/
            Queries/MySql/  Queries/SqlServer/
            Scripts/MySql/{StoredProcedures,Views,Triggers,Partitioning}
            Scripts/SqlServer/{...same...}
            Migrations/MySql/  Migrations/SqlServer/
            Bulk/ MySqlBulkWriter.cs  SqlServerBulkWriter.cs
Identity/   Pbkdf2PasswordHasher  JwtAccessTokenService  JwtOptions
Common/     SystemClock
Portal/     RJ/ JK/ MP/ UP/ CG/ TN/ HP/   client + Models/ + Mappers/ per state
Files/  Outbox/  BackgroundWorkers/  Secrets/
DependencyInjection.cs   AddInfrastructure() — the ONLY provider switch (Database:Provider = MySql | SqlServer)
```

## 3. Dependency rules

```
Api  →  Business  →  Domain          Infrastructure implements
                ↑                     interfaces declared in Business
        Infrastructure
```

| From | May reference |
|---|---|
| `Api` | `Business`, `Contracts`, `Logging`; `Infrastructure` **only** in `ApiServiceExtensions.cs` for DI |
| `Desktop` | `Contracts` only |
| `Business` | `Domain`, `Contracts` |
| `Infrastructure` | `Business`, `Domain`, `Contracts` |
| `Domain` / `Contracts` | nothing |

Architecture tests fail the build on: `Domain` referencing anything · `Business` referencing `Infrastructure`, EF Core, a DB driver, `HttpClient` or a portal type · `Api` referencing an excise implementation, portal client or `DbContext` · EF Core used outside `Infrastructure`.

When a state component needs a portal call it depends on an interface declared in `Business` (`IRjPortalClient`), implemented in `Infrastructure/Portal/RJ/`.

## 4. Where new code goes

| Adding | Location |
|---|---|
| HTTP endpoint | `Api/Controllers/` or `Controllers/Line/` |
| Use case / orchestration | `Business/<Feature>/<Feature>Service.cs` |
| Rule true in every state | `Domain/` |
| Rule that differs by state | `Business/Excise/<State>/`, behind a capability interface |
| DB read/write, SP, view | `Infrastructure/Database/` |
| Government portal call | `Infrastructure/Portal/<State>/` |
| Request/response type | `Contracts/<Module>/` |
| Line-app hardware code | `Desktop/` — HTTP to `/api/line`, never the database |
| Register a service | `Business/DependencyInjection.cs` (`AddBusiness`) or `Infrastructure/DependencyInjection.cs` (`AddInfrastructure`) |
| A value the customer can change | database configuration, not code |

## 5. Mandatory patterns

**Controller — always this shape, nothing more**

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
}
```

No business rules, SQL, portal calls, `try/catch` or state conditions in a controller.

**Interfaces — exactly two kinds, nothing else.**

1. **`I<Feature>Service` for every module** (`IAuthService`, `IBrandService`). Lives next to its implementation in the same feature folder. This is a convention, kept so every module has the same shape and API tests can inject a fake.
2. **An interface whose implementation lives in another project** (`IUserRepository`, `IAccessTokenService`, `IPasswordHasher`, the excise capabilities). Mandatory — Business may not reference Infrastructure, so the interface is the only way across.

Placement: an interface goes in the folder of the feature that **owns** it (`Business/Auth/IUserRepository.cs`). `Common/` is only for what three or more modules use (`IClock`, `ITenantContext`, `ICurrentUser`, `IPasswordHasher`). ⛔ Turning `Common/Abstractions` into a dumping ground for every module's interfaces.

**Domain declares no interfaces.** A Domain method that needs a limit or a rule takes it as a plain value (`user.RegisterFailedLogin(3)`), never as an injected service.

Any interface outside these two kinds waits until a second implementation actually exists.

**Tenant — never from the request.** Hierarchy Company → Excise → Supplier Code; there is no plant entity. `ITenantContext` carries `CompanyId, SupplierCodeId, ExciseCode`, filled by `SessionValidationMiddleware` from the supplier code picked for the session (`USER_SESSION.ACTIVE_SUPPLIER_CODE_ID`). `CompanyId` / `SupplierCodeId` / `ExciseCode` must never be request parameters.

**Rights — `[HasPermission]` on every endpoint** (except the auth flow and `/health`), with a key from `Business/Access/Permissions.cs` that also exists as a `PAGE_ACTIONS` row. Inside a service, ask `CurrentAccess` (`HasPermissionAsync`, `EnsureCanManageAdminsAsync`, `RequireSupplierCode`, `CompanyScopeAsync`) — never read roles yourself. Rights are read per request and are never put in the JWT. A new page adds its `PAGES` row (with `APPLICATION_TYPE` `WEB` or `LINE` — web application or line application) and its `PAGE_ACTIONS` rows (any action keys, e.g. `approve`) in its SQL script; nothing else changes. Full rules: `06-roles-rights-plan.md`.

**Status values vs lookup tables.** A value the code branches on is a C# `enum` stored as text in the row; a business category that grows as data gets its own table with a foreign key. ⛔ A generic LOOKUP table.

**State differences — capability, never a condition**

```csharp
// ⛔ forbidden outside Business/Excise/*
if (tenant.ExciseCode == "RJ") { ... }

// ✅ required
var provider = _capabilities.Resolve<IBrandProvider>(_tenant.ExciseCode);
var brands   = await provider.GetBrandsAsync(filter, ct);
```

Capability interfaces stay narrow and per-behaviour — never one large `IExciseService`.

**Responses** — success returns the payload itself (`200` read/update/action, `201` + `Location` create). **Every response has a body — never `204`**: an action on a record returns the record after the change, an action with no record returns `MessageResponse` → `{ "message": "..." }`. Lists return `PagedResponse<T>` → `{ items, page, pageSize, totalCount }`. Errors are ProblemDetails, produced only by `ExceptionMiddleware`:

```json
{ "type": "https://errors.stliquortnt.local/brand_duplicate", "status": 409, "errorCode": "BRAND_DUPLICATE",
  "title": "Brand already exists", "detail": "...", "instance": "/api/brands", "correlationId": "6f1c...",
  "errors": { "etin": ["Already used"] } }
```
Same seven keys on every error, in that order. `type`'s real target is still to be decided.

No `{ success, data, message }` envelope. Exceptions (all derive from `AppException`): `ValidationException` 400, `UnauthorizedException` 401, `ForbiddenException` 403, `NotFoundException` 404, `BusinessException` 409, `IntegrationException` 502 (added with first use) — each carrying a stable `errorCode` from `Business/Common/ErrorCodes.cs`. `DbException` becomes 500 `DATABASE_ERROR`. In every environment (owner decision, ADR 0001) a 500 carries the inner-exception chain in `detail`, plus `exceptionType` and stack frames. Framework refusals also get a body: `ENDPOINT_NOT_FOUND` 404, `METHOD_NOT_ALLOWED` 405, `UNSUPPORTED_MEDIA_TYPE` 415, `FORBIDDEN` 403.

**Logging** — indented JSON (`AppJsonFormatter`). Every entry has the same keys: `Timestamp` (IST), `Level`, `CorrelationId`, `Method`, `Message`, `Context`, plus `Exception` on errors. Put details in a scope field, not in the message; set `Method` only when the logging class is not the right name. Start-up and start-up failures are logged by `Program.cs`. How much is logged follows `SECURITY_CONFIG.LOG_MODE`: `NORMAL` gives one line per request plus errors; `DETAIL` adds request and response bodies, every Business method's input, output and exception (added automatically by `MethodLoggingProxy`, so write no logging code for it), and the SQL. It is switched live from the admin panel; never set levels in appsettings. Any other payload you log goes through `SafeJson`, which masks password, answer, token and hash fields. ⛔ Logging a secret, or a raw request body, directly.

**Stored procedures, views, triggers** — use them wherever the use case calls for them, under three fixed rules: they live only in `Infrastructure/Database/Scripts/<Provider>/`; they are called through an interface declared in `Business`, implemented once per provider; and they take `CompanyId` / `SupplierCodeId` / `ExciseCode` as parameters from `ITenantContext`, because EF's global tenant filter does **not** apply to them — that one is a security rule. Every object must exist in **both** provider folders.

**Portal integration** — production writes to the local database first, with an outbox row in the same transaction; a background worker sends it onward and retries. A production endpoint never waits for a portal response.

**Volume** — designed for ~100,000 cases and ~4,800,000 bottles per day. Never load a transaction table into memory; every list endpoint is server-paged (default 50, max 200) and projects to a response type; reads use `AsNoTracking()`; line writes go through `IBulkWriter`; a new query ships with its index.

## 6. Naming

| Item | Convention | Example |
|---|---|---|
| Domain entity | **exactly the DB table name (verbatim, CAPITAL)**; properties stay PascalCase, mapped in `IEntityTypeConfiguration` | table `USERS` → `USERS`; `SECURITY_CONFIG` → `SECURITY_CONFIG` |
| Controller / Business class | `<Noun>Controller` / `<Feature>Service` | `BrandController`, `BrandService` |
| State implementation | `<Code><Capability>` | `RjBrandProvider`, `MpCaseCodeGenerator` |
| Request / Response | `<Verb><Noun>Request`, `<Noun>Response` | `CreateBrandRequest`, `BrandResponse` |
| Provider query | `<Provider><Name>Query` | `MySqlProductionReportQuery` |
| SP / view in DB | `sp_<Operation>` / `vw_<Name>` | `sp_GetProductionReport` |
| Route | all lowercase, words joined — **no `-`**, plural, no version segment | `/api/brands`, `/api/line/casecodes`, `/api/auth/changepassword` |
| Permission key | `<module>.<action>` | `brand.insert`, `dispatch.send` |
| Async method | `...Async`, always takes `CancellationToken` | `GetBrandsAsync` |
| Test | `Method_Scenario_Expectation` | `Allocate_WhenTwoLines_ReturnsDistinctBlocks` |

## 7. Line API (production-line machines)

Same application, separate area `/api/line`, separate auth scheme (machine key + numeric PIN), separate Swagger document.

- Every write carries an `Idempotency-Key`; a repeated key returns the original result and writes nothing.
- Case codes are handed out in **blocks** (default 500), allocated atomically from a counter table with an `UPDATE ... WHERE` row lock. Provider-neutral — never SQL Server `SEQUENCE`.
- Aggregation writes are bulk through `IBulkWriter`, never one call per bottle.
- Aggregation delete is soft, and a deleted aggregation still appears in the Case Report.
- Every request sends the line app build number; a mismatch returns `409` with a clear message.

## 8. Never do this

| ⛔ | Instead |
|---|---|
| `if (excise == "RJ")` outside `Business/Excise/*` | capability interface + state implementation |
| A controller, route or API per state | one common API for all states |
| Tenant values taken from the request | `ITenantContext` |
| SP / view / raw SQL without tenant parameters | explicit tenant parameters from the context |
| SQL text or SP names in `Business` or `Api` | interface in `Business`, implementation in `Infrastructure` |
| EF Core or `HttpClient` types in `Business` or `Domain` | interfaces declared in `Business` |
| `{ success, data, ... }` response envelope | payload + status code; ProblemDetails for errors |
| Portal model types reaching `Business` or `Contracts` | map inside `Infrastructure/Portal/<State>/` |
| A production endpoint awaiting a portal response | local write + outbox |
| Different business behaviour per database provider | same result; only the query may differ |
| A feature shipped for one provider only | both providers, same change |
| `try/catch` in a controller to shape a response | `ExceptionMiddleware` |
| A generic repository, or an interface invented for its own sake | see the interface rule above; anything beyond it waits for a second implementation |
| Microservices, event bus, a database per state | one modular monolith, one database |

## 9. Checklist for a new module

1. Module name matches the frontend feature name; add permission keys (and a feature-flag key if the module is optional).
2. `Contracts/<Module>/` — request and response types.
3. `Domain/` — entity and its rules, if the module owns a business object.
4. `Business/<Module>/<Module>Service.cs` + validators; declare any interface Infrastructure must implement.
5. State variation → capability interface in `Business/Excise/` + per-state implementations. No conditions in the service.
6. `Infrastructure/` — EF configuration and queries; SP/view scripts in **both** provider folders if used.
7. `Api/Controllers/<Module>Controller.cs` — thin, `[HasPermission]` on every endpoint.
8. Tests: unit for rules, integration for data access (both providers), API for endpoints, one tenant-isolation test.
9. `dotnet build ST.LiquorTNT.sln` and `dotnet test ST.LiquorTNT.sln` green, architecture tests included.

**Concrete reference:** the `Auth` module is the finished example of every rule above — `Contracts/Auth`, `Domain/Entities/User.cs`, `Business/Auth/*`, `Infrastructure/Database/Configurations/UserConfiguration.cs` + `Repositories/UserRepository.cs` + `Identity/*`, `Api/Controllers/AuthController.cs`, and the tests in `Domain.Tests` and `Business.Tests`. Copy its shape.

---

*Detail, rationale and open decisions: `02-backend-architecture-standards.md`. File-by-file map: `03-solution-structure.md`.*
