# Solution Structure — `ST.LiquorTNT` (as it exists on 2026-09-28)

> Every file in the solution today, what it does, and where the next things go. Companion to `02-backend-architecture-standards.md` §3. Location: `C:\Projects\Liquor_Application`.

---

## 1. Dependency direction

```
Api  →  Business  →  Domain            Infrastructure implements the interfaces
                ↑                       declared in Business
        Infrastructure

Logging   → (Serilog only)             Api uses it for host logging
Desktop   → Contracts only             the line app; talks HTTP to /api/line
Contracts → nothing                    wire format shared by Api, Business, Desktop, React
```

| Project | May reference |
|---|---|
| `Api` | `Business`, `Contracts`, `Logging`; `Infrastructure` **only** inside `Extensions/ApiServiceExtensions.cs` |
| `Business` | `Domain`, `Contracts` |
| `Infrastructure` | `Business`, `Domain`, `Contracts` |
| `Logging` | nothing (Serilog packages) |
| `Desktop` | `Contracts` |
| `Domain`, `Contracts` | nothing |

`tests/ST.LiquorTNT.Architecture.Tests/LayerRuleTests.cs` (7 rules) fails the build if any of these is broken — including "Api references Infrastructure only in `Extensions`".

**Which layer holds what — fixed, no overlap:**

| Thing | Layer | Folder |
|---|---|---|
| Interfaces (`I<Feature>Service`, repositories, token/hash services) | Business | the feature folder that owns it |
| Services (use cases) | Business | `Business/<Feature>/` |
| Request / response models | Contracts | `Contracts/<Feature>/` |
| Entities and rules — **class name = table name** | Domain | `Domain/Entities/` — no interfaces in Domain |
| Repositories, EF configuration, DbContext, hashing, JWT, audit writer, provider switch | Infrastructure | `Infrastructure/Database/`, `Identity/`, `Audit/` |
| Controllers, middleware, claims/request reading, DI wiring | Api | `Api/Controllers/`, `Api/Middleware/`, `Api/Security/`, `Api/Extensions/` |

---

## 2. Root

| File | Purpose |
|---|---|
| `ST.LiquorTNT.sln` | 14 projects (13 .NET + the React `.esproj`) under solution folders `src` and `tests` |
| `Directory.Build.props` | `net8.0`, `LangVersion latest`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors` **only in Release** |
| `Directory.Packages.props` | `ManagePackageVersionsCentrally=true` — every package version lives here. ⛔ `Version=` on a `PackageReference` |
| `README.md` | run instructions, endpoints, error codes |
| `db/mysql/003_user_module_schema.sql` | User-module tables + seeds (`INSERT IGNORE`, safe to re-run for seeds) |
| `db/mysql/004_users_remove_excise_plant.sql` | drops `EXCISE_CODE`, `ALLOTED_PLANT_ID` from `USERS` (access mapping is a later module) |
| `db/mysql/005_user_module_phase2.sql` | `USER_LOG.USER_ID/EXCISE_CODE` nullable, `PASSWORD_RESET_REQUEST.REQUEST_TOKEN_HASH`, admin credential |
| `db/mysql/006_log_mode.sql` | `SECURITY_CONFIG.LOG_MODE` = `NORMAL` (the admin switches it to `DETAIL`) |
| `db/mysql/007_session_sliding.sql` | `USER_SESSION.ABSOLUTE_EXPIRES_AT` (hard limit) + `SECURITY_CONFIG.SESSION_IDLE_MINUTES` = 60 (sliding window) |
| `db/mysql/008_session_absolute_default.sql` | default for `USER_SESSION.ABSOLUTE_EXPIRES_AT` |
| `db/mysql/009_roles_rights.sql` | roles & rights: `LIQUOR_CATEGORY`, `SUPPLIER_CODE` (supplier codes), `PAGE_ACTIONS`, company-wise `ROLES` + templates, rebuilt `ROLE_RIGHTS`, `USER_ROLES`, `USER_RIGHTS`, `USER_SESSION.ACTIVE_SUPPLIER_CODE_ID`; moves `USERS.ROLE_ID` into `USER_ROLES` and `COMPANY.EXCISE_CODE/SUPPLIER_CODE` into `SUPPLIER_CODE` (docs/06) |
| `db/mysql/010_company_view.sql` | `company.view` permission (SYSTEM) for `GET /api/companies` |
| `db/mysql/demo_globus_spirits.sql` | DEMO data only (Globus Spirits, supplier codes RJ 772 / RJ 1028 / JK 369, one user per role) — never on a customer installation |

The `USERS`, `ROLES`, `PAGES`, `COMPANY`, `EXCISE`, `ALLOTEDPLANTS`, `USER_LOG` tables pre-exist in `st_tnt_liquor` (legacy merge); they are not created by these scripts.

---

## 3. `src/`

### `ST.LiquorTNT.Api` — Web API host

| File | What it does |
|---|---|
| `Program.cs` | Clears the inbound claim map, `UseAppLogging()` (two-stage: start-up and start-up failures are logged), `API starting` entry, `AddApiServices()`, pipeline: `Correlation → RequestLogging → Exception → CsrfProtection → Swagger(dev) → CORS → Authentication → SessionValidation → Authorization → MapControllers → /health`. `public partial class Program` for `WebApplicationFactory`. |
| `Extensions/ApiServiceExtensions.cs` | Controllers (model-binding failures become `ValidationException` so every error is ProblemDetails), `ICurrentUser`/`ITenantContext`/`IRequestContext`, `AddBusiness()`, `AddInfrastructure()`, JWT bearer validation (Authorization header first, else the `jwt` cookie), CORS (exact origins, credentials), `Auth:Cookie` options, Swagger. **Only file allowed to reference Infrastructure.** |
| `Middleware/CorrelationMiddleware.cs` | `X-Correlation-Id` in, echoed out, pushed into Serilog. |
| `Middleware/ExceptionMiddleware.cs` | The only place errors become HTTP (RFC 7807 + `errorCode` + `correlationId`); also fills bare framework statuses (404/405/415/403) and returns exception chain, type and stack in every environment (ADR 0001); keeps headers set upstream. |
| `Middleware/CsrfProtectionMiddleware.cs` | Browser CSRF guard: on POST/PUT/PATCH/DELETE to a non-anonymous endpoint that carries the `jwt` cookie and no Authorization header, `X-XSRF-TOKEN` must equal the `XSRF-TOKEN` cookie and a present `Origin` must be this host or a CORS origin → else `403 CSRF_REJECTED`. Bearer callers are never checked. |
| `Middleware/SessionValidationMiddleware.cs` | After JWT auth: the token's session must be `ACTIVE` and inside its deadline in `USER_SESSION`. Slides the idle deadline (at most one write a minute; reads `SESSION_IDLE_MINUTES` only then). Ends it with `SESSION_TIMED_OUT` (idle → login page), `SESSION_EXPIRED` (hard limit → password popup + retry) or `SESSION_INVALID` (logged out / revoked), and audits the expiry. Also loads the supplier code picked for the session and hands it to `TenantContext` as `SessionScope`. |
| `Security/CurrentUser.cs` · `TenantContext.cs` · `RequestContext.cs` · `SessionScope.cs` | Claims (`sub`, `unique_name`, `company_id`), the session's supplier code (company / supplier code / excise) and request facts (IP, user agent, correlation id, the token from the bearer header or else the `jwt` cookie) for Business. |
| `Security/AuthCookies.cs` | The browser's cookies, issued at login and cleared at logout: `jwt` (HttpOnly) and `XSRF-TOKEN` (readable); Secure on https/localhost, SameSite from `Auth:Cookie:SameSite` (Lax), Path=/, Max-Age = session hard limit. |
| `Security/HasPermissionAttribute.cs` · `PermissionAuthorization.cs` | `[HasPermission(Permissions.X)]` → policy built on the fly (`PermissionPolicyProvider`) → `PermissionHandler` asks `CurrentAccess`; refusals are `409 SUPPLIER_CODE_NOT_SELECTED` / `403 PERMISSION_DENIED` |
| `Controllers/AuthController.cs` | `api/auth`: `login` (token in the body + the two cookies), `logout` (ends the header's or cookie's session, clears the cookies), `me`, `changepassword` (anonymous, password-verified), `sessions`, `sessions/{id}`, `mysuppliercodes`, `selectsuppliercode`, `mypermissions` |
| `Controllers/PasswordResetController.cs` | `api/auth/forgotpassword`: `start`, `verify`, `reset` (anonymous) |
| `Controllers/SecurityQuestionsController.cs` | `api/securityquestions`: list (anonymous), `mine` (PUT) |
| `Controllers/UsersController.cs` | `api/users`: create (201 + Location), get, paged list, update, activate, deactivate, unlock, `{id}/access`, `{id}/roles`, `{id}/rights` |
| `Controllers/RolesController.cs` · `PagesController.cs` | `api/roles` (CRUD + `{id}/rights` grid), `api/pages` (every page with its actions; `?applicationType=WEB` or `LINE`) |
| `Controllers/SupplierCodesController.cs` · `LiquorCategoriesController.cs` | `api/suppliercodes`, `api/liquorcategories`: list / get for the company; create / edit / activate for Super Admin |
| `Controllers/CompaniesController.cs` · `ExcisesController.cs` | `api/companies` (Super Admin, `company.view`), `api/excises` (`suppliercode.view`) — dropdown lists |
| `Controllers/SecurityConfigController.cs` · `PasswordPoliciesController.cs` | `api/securityconfig`, `api/passwordpolicies`: admin read/update of `SECURITY_CONFIG` and `PASSWORD_POLICY` |
| `appsettings.json` | `ConnectionStrings:Default`, `Database:{Provider,ServerVersion}`, `Jwt:{Issuer,Audience,SigningKey}`, `Cors`, `Serilog` (Console and File sinks with `AppJsonFormatter`, indented JSON; levels are **not** set here, they follow `LOG_MODE`). No security behaviour here, it lives in the database. |
| `Middleware/RequestLoggingMiddleware.cs` | One entry per `/api` call with its errorCode (NORMAL); DETAIL adds the query string and the request and response bodies for `/api` calls (masked, size-capped) |
| `Logging/MethodLoggingProxy.cs` · `MethodLoggingRegistration.cs` | Wraps every Business `I…Service`: in DETAIL each call logs method, input, output, duration, and the refusal or exception |
| `Logging/LogModeRefresher.cs` | Background service: reads `SECURITY_CONFIG.LOG_MODE` at start-up and every 10 s, and applies it to `LogModeSwitch`. The first read logs `Database reachable` or `Database check failed`. |
| `Logging/StartupFacts.cs` | Content of the `API starting` entry: version, environment, machine, DB provider / server / name (never the connection string) |

All controllers are thin (one call per action, no `try/catch`) and every action returns a body (`200`/`201`, never `204`). Routes are lowercase with no `-`. Every action carries `[HasPermission(...)]` except the auth flow and `/health`.

### `ST.LiquorTNT.Business` — use cases, organised by feature

| Folder / file | What it does |
|---|---|
| `DependencyInjection.cs` | `AddBusiness()`: services below + `PasswordPolicyValidator`, `PasswordRules`, all FluentValidation validators |
| `Common/IClock.cs` | `UtcNow` (JWT) and `IndiaNow` (everything in the User module — IST) |
| `Common/ICurrentUser.cs` · `ITenantContext.cs` · `IRequestContext.cs` | filled by the Api layer |
| `Common/SecuritySettings.cs` | typed, defaulted view of `SECURITY_CONFIG` (`FromEntries` is pure; nonsense values fall back) |
| `Common/UserLogEntry.cs` | what to audit (`UserLogActions`, `UserLogModules`) |
| `Common/ValidationExtensions.cs` | FluentValidation result → 400 with camelCase field keys |
| `Common/ErrorCodes.cs` · `Exceptions/` | stable codes; `AppException` family (400/401/403/404/409) |
| `Common/Abstractions/` | cross-module interfaces implemented in Infrastructure: `IPasswordHasher`, `ISecurityConfigProvider`, `IUserLogWriter` |
| `Users/` (access) | `IUserAccessService`/`UserAccessService` (roles / custom rights per supplier code), `UserAccessRules` (company scope, admin-user protection, no self-change, grant scope), `IUserAccessRepository` |
| `Users/` | `IUserService`/`UserService`, `IUserRepository`, `IReferenceLookup` (role/company exist), `IPasswordPolicyRepository`, `PasswordPolicyValidator` (pure rule check), `PasswordRules` (strictest policy across the user's roles + history, shared by create/change/reset), `UserProjections` (the one USERS → `UserResponse` expression), request validators |
| `Auth/` | `CredentialVerifier` (the one password/answer check: same answer for unknown user and wrong password, counts every wrong attempt, refuses inactive/blocked/locked), `IAuthService`/`AuthService` (login, logout, me, change-password), `ISessionService`/`SessionService`, `ISecurityQuestionService`, `IPasswordResetService`, `ISessionRepository`, `ISecurityQuestionRepository`, `IAccessTokenService`, `ITokenHasher`, `SecurityAnswers`/`ResetTokens`, validators |
| `SecurityConfig/` | `ISecurityConfigService`/`SecurityConfigService` (+ `ISecurityConfigRepository`) — admin editing with per-`DATA_TYPE` validation |
| `PasswordPolicies/` | `IPasswordPolicyService`/`PasswordPolicyService` + validator — admin editing of policy rules |
| `Access/` | `CurrentAccess` (the one "may the caller do this?" answer, per request), `Permissions` (every key), `IAccessService`/`AccessService` (my supplier codes, select supplier code, my permissions), `SupplierCodeDirectory`, `IAccessRepository` |
| `Roles/` | `IRoleService`/`RoleService` (roles + rights grid, admin-role and grant-scope rules), `RoleTemplates` (default roles into a new company), `IRoleRepository`, validators |
| `SupplierCodes/` · `LiquorCategories/` | supplier code and category masters (Super Admin edits; first supplier code copies the default roles) |
| `Companies/` · `Excises/` | read-only lists (`ICompanyService`, `IExciseService` + repositories) |

Next here: `Companies/`, `Brands/`, `Batches/`…, `Excise/` capability interfaces, `Line/`.

### `ST.LiquorTNT.Domain` — entities and rules (references nothing)

| Entity (= table) | Rules it owns |
|---|---|
| `USERS` | `Create`, `UpdateProfile`, `SetPassword` (+ history row), `RegisterFailedLogin` (IST day, lock, no sliding lock), `RegisterSuccessfulLogin`, `IsLockedAt`, `IsPasswordExpiredAt`, `Unlock`, `Activate`/`Deactivate` |
| `USER_PASSWORD_HISTORY` | appended by `USERS.SetPassword` only |
| `USER_SESSION` | `Create` (idle window + hard limit), `SelectSupplierCode`, `IsActiveAt`, `Slide` (activity; never past the hard limit), `ReachedLimitAt`, `Logout`/`Revoke`/`Expire` |
| `PASSWORD_POLICY` | `Create`/`UpdateRules` (guards), `ExpiryFrom`, `Strictest` (several roles) |
| `ROLES` | `Create` (company role or template), `CopyOf` (template → company), `Update` (Super Admin never) |
| `ROLE_RIGHTS`, `USER_ROLES`, `USER_RIGHTS`, `ROLE_PASSWORD_POLICY` | `Create` (+ `ChangePolicy`) |
| `SUPPLIER_CODE`, `LIQUOR_CATEGORY` | `Create`, `Update`, `SetActive` |
| `PAGES` (`ApplicationType` WEB / LINE from `Rules/`), `PAGE_ACTIONS` (`GrantScope` from `Rules/`), `EXCISE`, `COMPANY`, `SECURITY_QUESTION` | read models for lookups |
| `USER_SECURITY_QUESTION` | `Create` (hash only), `Deactivate` |
| `PASSWORD_RESET_REQUEST` | `Create`, `RegisterFailedVerify`, `MarkVerified`/`MarkUsed`/`MarkExpired`, `IsExpiredAt`, `IsOpen` |
| `SECURITY_CONFIG` | `UpdateValue` |
| `USER_LOG` | `Create` (append-only) |
| `Exceptions/DomainException.cs` | reserved for invariants; guards use `ArgumentException` |

Private setters, private constructors, behaviour through methods, limits passed as plain values. **No interfaces, no EF attributes.**

### `ST.LiquorTNT.Contracts` — wire format (references nothing)

`Auth/` (`LoginRequest/Response`, `CurrentUserResponse`, `ChangePasswordRequest`, `SessionResponse`, security-question and forgot-password requests/responses), `Users/` (`CreateUserRequest` with `roles`, `UpdateUserRequest`, `UserListRequest`, `UserResponse`, `UserAccessContracts`), `Access/` (`SupplierCodeResponse`, `SelectSupplierCodeRequest`, `MyPermissionsResponse`), `Roles/`, `SupplierCodes/`, `LiquorCategories/`, `SecurityConfig/`, `PasswordPolicies/`, `Common/PagedResponse.cs`, `Common/MessageResponse.cs` (body of an action with no record to return). All timestamps are IST.

### `ST.LiquorTNT.Infrastructure`

| File | What it does |
|---|---|
| `DependencyInjection.cs` | `AddInfrastructure()`: `AddDbContext<AppDbContext>` with the **only provider switch**; registers every repository, `ISecurityConfigProvider`, `IUserLogWriter`, `IPasswordHasher`, `IAccessTokenService`, `ITokenHasher`, `IClock`, `JwtOptions` |
| `Database/AppDbContext.cs` | the only `DbContext`; one `DbSet` per entity, named like the table |
| `Database/Configurations/*Configuration.cs` | one `IEntityTypeConfiguration<T>` per entity: CAPITAL column names, lengths, indexes, `USERS.USERNAME` collation explicit (case-insensitive) |
| `Database/Repositories/` | `UserRepository` (paged projection, recent hashes, unique-violation → 409), `ReferenceLookup`, `PasswordPolicyRepository`, `SessionRepository`, `SecurityQuestionRepository`, `SecurityConfigRepository`, `AccessRepository` (effective rights per supplier code, supplier codes of a user), `RoleRepository`, `UserAccessRepository`, `SupplierCodeRepository`, `LiquorCategoryRepository`, `CompanyRepository`, `ExciseRepository`, `SupplierCodeQuery` (supplier code + names + "RJ CL 550") |
| `Database/DbErrors.cs` | provider error-code mapping in one place (MySQL 1062 today) |
| `Audit/UserLogWriter.cs` | stages a `USER_LOG` row with request facts; committed by the caller's `SaveChanges` |
| `Identity/Pbkdf2PasswordHasher.cs` · `JwtAccessTokenService.cs` · `JwtOptions.cs` · `Sha256TokenHasher.cs` | credentials and tokens |
| `Common/SystemClock.cs` · `SecurityConfigProvider.cs` | IST clock (cross-OS zone lookup); `SECURITY_CONFIG` → `SecuritySettings` per request |

Next here: `Database/Interceptors/` (tenant filter), `Queries/<Provider>/`, `Scripts/<Provider>/`, `Migrations/`, `Portal/<State>/`, `Outbox/`.

### `ST.LiquorTNT.Logging`, `ST.LiquorTNT.Desktop`, `ST.LiquorTNT.WebApplication`

Logging: `LoggingSetup` (Serilog sinks from configuration, levels bound to `LogModeSwitch`), `LogModeSwitch` (NORMAL / DETAIL, switched live), `AppJsonFormatter` (one JSON object per line, IST `time`, readable `message`), `SafeJson` (masks password / answer / token / hash before anything is logged), `LogFields`. Log files: `logs/stliquortnt-YYYYMMDD.json`, rolled daily and at 100 MB, 30 kept. Desktop: WinForms shell referencing `Contracts` only; WebApplication: the React app (Vite + TypeScript) as `ST.LiquorTNT.WebApplication.esproj` in the `.sln`; its own docs are in `src/ST.LiquorTNT.WebApplication/README.md` and `docs/`.

---

## 4. `tests/`

| Project | What it covers |
|---|---|
| `Domain.Tests` (74) | `USERS` lockout matrix (N-th attempt, same/new IST day to the second, expired lock restart, no sliding lock, correct password while locked), password state, sessions, reset requests, policies (incl. strictest of several), config, `ROLES` (templates, copy, Super Admin) |
| `Business.Tests` (215) | `CurrentAccess` + `AccessService` (supplier code pick), `RoleService` + `RoleTemplates`, `UserAccessService` (admin protection, no self-change, grant scope), `UserService` (company scope, roles, strictest policy), `AuthService` (login matrix incl. "locked now" vs "already locked", session limit, change-password counts as attempt), `SessionService`, `PasswordResetService`, `SecurityQuestionService`, `SecurityConfigService`, `PasswordPolicyService`, `PasswordPolicyValidator`, `SecuritySettings`, request validators — all with the in-memory fakes in `Fakes/` |
| `Infrastructure.Tests` (21) | PBKDF2, SHA-256, JWT claims; EF mappings and repositories against MySQL (`ST_TNT_TEST_CONNECTION` or the dev server), incl. duplicate user name → 409, and `AccessRepositoryTests` (rights per supplier code, "all supplier codes" never leak into another company) |
| `Api.Tests` (104) | `WebApplicationFactory` end-to-end: login → 3 wrong = lock → unlock (200 + user) → logout kills token → session limit → revoke → deactivate; forced password change; forgot-password start/verify/reset; admin config validation; error shape and correlation header; unknown route / wrong method / non-JSON, empty or broken JSON body still get ProblemDetails. `ExceptionMiddlewareTests`, `SessionValidationMiddlewareTests` (idle → TIMED_OUT, hard limit while working → EXPIRED, one write a minute), an expired-JWT → `SESSION_EXPIRED` check, `RolesRightsFlowTests` (supplier code → default roles → Agent Manager → rights apply on the next call; `SUPPLIER_CODE_NOT_SELECTED`) and `Logging/` (JSON formatter, secret masking, log-mode switch, request logging incl. malformed bodies, method-logging proxy and its registration) as unit tests. One xUnit collection (shared admin account). |
| `Architecture.Tests` (7) | the layer rules in §1 |
| `Edge.Tests` (1) | line application placeholder |

---

## 5. Running it

```
# 1. database (once, in order) — the base tables already exist
mysql -h 192.168.1.99 -u root -p st_tnt_liquor < db\mysql\003_user_module_schema.sql
mysql -h 192.168.1.99 -u root -p st_tnt_liquor < db\mysql\004_users_remove_excise_plant.sql
mysql -h 192.168.1.99 -u root -p st_tnt_liquor < db\mysql\005_user_module_phase2.sql
mysql -h 192.168.1.99 -u root -p st_tnt_liquor < db\mysql\006_log_mode.sql

# 2. build + tests (Infrastructure/Api tests need the database)
dotnet build ST.LiquorTNT.sln
dotnet test  ST.LiquorTNT.sln

# 3. run
dotnet run --project src\ST.LiquorTNT.Api        # Swagger at /swagger in Development

# 4. login
POST /api/auth/login   { "userName": "admin", "password": "Admin@123" }
GET  /api/auth/me      Authorization: Bearer <accessToken>
```

---

## 6. Where the next file goes — quick answer

| Adding | Put it in |
|---|---|
| Endpoint | `Api/Controllers/<Module>Controller.cs` (or `Controllers/Line/`) |
| Use case | `Business/<Module>/<Module>Service.cs` + `I<Module>Service.cs` |
| Interface Infrastructure must implement for that module | `Business/<Module>/I<Name>.cs` |
| Interface used by 3+ modules and implemented in Infrastructure | `Business/Common/Abstractions/` |
| Rule true in every state | `Domain/Entities/<TABLE_NAME>.cs` method |
| Rule that differs by state | `Business/Excise/<State>/`, behind a capability interface |
| Request / response | `Contracts/<Module>/` |
| EF mapping | `Infrastructure/Database/Configurations/` |
| Repository / query | `Infrastructure/Database/Repositories/` or `Queries/<Provider>/` |
| SP / view / trigger | `Infrastructure/Database/Scripts/MySql/…` **and** `Scripts/SqlServer/…` |
| Audit of a business action | `UserLogEntry.Success/Failed(...)` via `IUserLogWriter`, before the `SaveChanges` |
| A value a customer may change | `SECURITY_CONFIG` / `PASSWORD_POLICY` (or the module's own config table), not code |
