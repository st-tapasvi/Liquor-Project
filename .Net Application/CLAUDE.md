# CLAUDE.md — ST.LiquorTNT

Excise Track & Trace merge platform: one ASP.NET Core 8 Web API + React replacing 11 state-wise WinForms server apps for liquor excise in India. Multi-tenant (Company → Plant → Excise), on-premise, MySQL primary / SQL Server supported.

## Before writing any code, read (in `docs/`)

1. `docs/00-START-HERE.md` — orientation and the five rules that matter most
2. `docs/04-ai-coding-guide.md` — the condensed rules; follow them literally
3. `docs/02-backend-architecture-standards.md` — full standard (v1.2); the source of truth when the guide is silent
4. `docs/03-solution-structure.md` — every file today and where the next one goes
5. `docs/01-project-context.md` — business context, the 11 states, open decisions

## Hard rules (the build enforces most of these)

- Layers: `Api → Business → Domain`; `Infrastructure` implements interfaces declared in `Business`. `Domain`/`Contracts` reference nothing. `Desktop` references only `Contracts`. `Infrastructure` is referenced by `Api` **only** in `Extensions/ApiServiceExtensions.cs`.
- The business layer project is **`ST.LiquorTNT.Business`** — never call it "Application". Do not create `ST.LiquorTNT.Integration` or `ST.LiquorTNT.Shared`.
- Interfaces go in `Business/<Feature>/` next to the service that uses them. `Business/Common/Abstractions/` only for interfaces used by 3+ modules and implemented in Infrastructure. **No interfaces in Domain.** No interface without a real second implementation or a project boundary.
- Tenant values (`CompanyId`, `PlantId`, `ExciseCode`) come only from `ITenantContext` (JWT claims). Never from a request. Always explicit parameters to SPs/views.
- State variation = capability interface in `Business/Excise/` + per-state class. `if (excise == "XX")` in common code is a defect.
- Errors: throw an `AppException` subclass with a code from `Business/Common/ErrorCodes.cs`; `ExceptionMiddleware` is the only place that shapes HTTP errors (RFC 7807). No `try/catch` in controllers. No `{success,data}` envelope.
- SQL objects live in `Infrastructure/Database/Scripts/MySql/` **and** `Scripts/SqlServer/`; called through a Business interface with one caller class per provider. Views for anything that must still filter/page.
- Package versions only in `Directory.Packages.props` (central package management).
- Domain entity class name = the exact DB table name (`USERS`, `USER_SESSION`, `PASSWORD_POLICY`); properties stay PascalCase and are mapped in the `IEntityTypeConfiguration`.
- Security behaviour (lock attempts, lock minutes, session limit/expiry, password rules) is read from `SECURITY_CONFIG` / `PASSWORD_POLICY` via `ISecurityConfigProvider` / `PasswordRules` — never a code constant. All User-module timestamps are IST via `IClock.IndiaNow`.
- Logs are JSON (`AppJsonFormatter`). How much is logged follows `SECURITY_CONFIG.LOG_MODE` (NORMAL / DETAIL, switched live), never an appsettings level. Business services are logged automatically by `MethodLoggingProxy`; anything else that logs a payload passes it through `SafeJson`, so passwords, answers, tokens and hashes never reach the log.
- Every new module ships with: Contracts, Business service + validator, Infrastructure mapping, thin controller, unit tests, and passes `ST.LiquorTNT.Architecture.Tests`.
- Reference implementation to copy: the `Users` and `Auth` modules (`Business/Users`, `Business/Auth`, their repositories, controllers and tests).

## Commands

```
dotnet build ST.LiquorTNT.sln
dotnet test  ST.LiquorTNT.sln
dotnet run --project src\ST.LiquorTNT.Api
```

DB: MySQL `st_tnt_liquor` @ `192.168.1.99` (`USERS`/`ROLES`/`COMPANY`/`USER_LOG` pre-exist; User-module scripts `db\mysql\003`–`006`, run once, seeds are `INSERT IGNORE`). Login `admin` / `Admin@123`. Infrastructure and Api tests need this database (override with `ST_TNT_TEST_CONNECTION`).

## Working style

- Hinglish is fine in conversation; code, comments, commits and docs stay in English.
- Keep `docs/` in sync with decisions: any change to layers, naming, response format, dual-provider rules or the capability interface set updates `02-backend-architecture-standards.md` and `04-ai-coding-guide.md` together.
- Do not leave stub or "just in case" files anywhere — the owner has asked for this explicitly.
