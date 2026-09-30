# ST.LiquorTNT

Merged Excise Track & Trace platform. One installation serves multiple companies, plants and excise
states. This repository holds the backend, the React front end and the line (desktop) application.

## Solution

```
Liquor_Application/
├─ ST.LiquorTNT.sln
├─ Directory.Build.props        net8.0, nullable, warnings-as-errors in Release
├─ Directory.Packages.props     every NuGet version in one place
├─ db/mysql/                    User-module schema scripts (003–005), run once in order
├─ docs/                        architecture standard, solution map, AI coding guide
├─ src/
│  ├─ ST.LiquorTNT.WebApplication   React (Vite + TS) - NOT in the solution
│  ├─ ST.LiquorTNT.Domain           entities (named after their tables) + rules; references nothing
│  ├─ ST.LiquorTNT.Business         use cases; declares the interfaces it needs
│  ├─ ST.LiquorTNT.Contracts        request/response types; references nothing
│  ├─ ST.LiquorTNT.Infrastructure   EF Core + MySQL, hashing, JWT, audit writer
│  ├─ ST.LiquorTNT.Logging          Serilog host setup, log field names
│  ├─ ST.LiquorTNT.Api              controllers, middleware, auth, Swagger  (startup project)
│  └─ ST.LiquorTNT.Desktop          line application shell (WinForms, net8.0-windows)
└─ tests/
   ├─ ST.LiquorTNT.Domain.Tests           entity rules (lockout, sessions, reset requests, policies)
   ├─ ST.LiquorTNT.Business.Tests         services with in-memory fakes
   ├─ ST.LiquorTNT.Infrastructure.Tests   hashing, JWT, EF mappings and repositories against MySQL
   ├─ ST.LiquorTNT.Api.Tests              end-to-end HTTP flows (WebApplicationFactory + MySQL)
   ├─ ST.LiquorTNT.Edge.Tests             line application tests (placeholder)
   └─ ST.LiquorTNT.Architecture.Tests     layer rules, enforced by the build
```

## Dependency rules

```
Api  →  Business    →  Domain            Infrastructure implements the
                 ↑                        interfaces declared in Business
         Infrastructure
```

| Project | May reference |
|---|---|
| `Api` | `Business`, `Contracts`, `Logging`; `Infrastructure` **only** in `ApiServiceExtensions` for DI |
| `Business` | `Domain`, `Contracts` |
| `Infrastructure` | `Business`, `Domain`, `Contracts` |
| `Logging` | nothing (Serilog only) |
| `Domain` / `Contracts` | nothing |
| `Desktop` | `Contracts` only - it talks to the API over HTTP, never to the database |

`ST.LiquorTNT.Architecture.Tests` fails the build if any of these is broken.

## 1. Database

MySQL `st_tnt_liquor` on `192.168.1.99`. The `USERS`, `ROLES`, `COMPANY`, `EXCISE` and `USER_LOG`
tables already exist there (legacy merge). The User module adds its tables with these scripts, run
once in order:

```
db/mysql/003_user_module_schema.sql       SECURITY_CONFIG, PASSWORD_POLICY, ROLE_PASSWORD_POLICY,
                                          USER_PASSWORD_HISTORY, USER_SESSION, SECURITY_QUESTION,
                                          USER_SECURITY_QUESTION, PASSWORD_RESET_REQUEST + seeds
db/mysql/004_users_remove_excise_plant.sql drops EXCISE_CODE / ALLOTED_PLANT_ID from USERS
db/mysql/005_user_module_phase2.sql       USER_LOG nullable columns, reset-token column, admin credential
db/mysql/006_log_mode.sql                 SECURITY_CONFIG.LOG_MODE (NORMAL / DETAIL)
db/mysql/007_session_sliding.sql          USER_SESSION.ABSOLUTE_EXPIRES_AT, SESSION_IDLE_MINUTES (sliding web sessions)
```

Seeds use `INSERT IGNORE`, so re-running never overwrites values an administrator has changed.

| | |
|---|---|
| user name | `admin` |
| password | `Admin@123` |

Passwords are PBKDF2-SHA256 (100,000 iterations, per-user salt); the database never holds a
readable password. For a customer install set `FORCE_PASSWORD_CHANGE = 1` on the admin row.

## 2. Configuration

Security behaviour is **data**, edited from the admin panel (`/api/securityconfig`,
`/api/passwordpolicies`) and stored in `SECURITY_CONFIG` / `PASSWORD_POLICY` — never in code:
failed-login attempts and lock duration, session limit and expiry, security-question switches,
password rules per role.

`src/ST.LiquorTNT.Api/appsettings.json` holds only host settings:

- `ConnectionStrings:Default` - `192.168.1.99 / st_tnt_liquor`
- `Database:Provider` / `ServerVersion` - MySQL today; SQL Server is a supported target
- `Jwt:Issuer`, `Jwt:Audience`, `Jwt:SigningKey` - **change the key on every installation**, ≥ 32 characters
- `Cors:AllowedOrigins` - the React dev server
- `Serilog` - sinks only (console + `logs/stliquortnt-YYYYMMDD.json`, JSON, IST time). How much is
  logged is **not** set here. It is `SECURITY_CONFIG.LOG_MODE`: `NORMAL`, or `DETAIL` for request and
  response bodies, method input/output and SQL. It is switched from the admin panel and applies
  within 10 s. Secrets are always masked.

All User-module timestamps are **IST** (customers are in India); the API returns them without an offset.

## 3. Run

```
dotnet restore
dotnet build ST.LiquorTNT.sln
dotnet test  ST.LiquorTNT.sln              # Infrastructure + Api tests need the MySQL above
dotnet run --project src/ST.LiquorTNT.Api
```

Swagger opens at `https://localhost:7180/swagger` in Development.
Set `ST_TNT_TEST_CONNECTION` to point the database tests at another MySQL.

## 4. Endpoints (User module)

| Area | Endpoints |
|---|---|
| Login | `POST /api/auth/login`, `POST /api/auth/logout`, `GET /api/auth/me`, `POST /api/auth/changepassword` (password-verified, so it also serves a forced first-login change) |
| Sessions | `GET /api/auth/sessions`, `DELETE /api/auth/sessions/{id}` |
| Recovery | `GET /api/securityquestions`, `PUT /api/securityquestions/mine`, `POST /api/auth/forgotpassword/start` → `/verify` → `/reset` |
| Users | `POST/GET /api/users`, `GET/PUT /api/users/{id}`, `POST /api/users/{id}/activate` `/deactivate` `/unlock` |
| Admin | `GET/PUT /api/securityconfig`, `GET/PUT /api/passwordpolicies` |
| Host | `GET /health` |

A login returns `{ accessToken, expiresAt, idleTimeoutMinutes, user }`. The session slides: every call restarts the idle window (`SESSION_IDLE_MINUTES`, 60), up to the hard limit `expiresAt` (`SESSION_EXPIRY_MINUTES`, 24 h). The token is a JWT **and** a row in `USER_SESSION`
(only its hash is stored): logout, revocation, deactivation, a password change or the configured
expiry make it unusable immediately.

Failures are RFC 7807 ProblemDetails with a stable `errorCode` and `correlationId` - never a
`{ success: false }` envelope. The main codes:

| Case | Status | errorCode |
|---|---|---|
| Field validation, weak password (`errors.password[]`) | 400 | `VALIDATION_FAILED` |
| Unknown user **or** wrong password | 401 | `INVALID_CREDENTIALS` |
| Session logged out / revoked | 401 | `SESSION_INVALID` (login page) |
| No call for the idle window | 401 | `SESSION_TIMED_OUT` (login page) |
| Hard limit reached, even while working | 401 | `SESSION_EXPIRED` (password popup, then retry the call) |
| N wrong passwords in one IST day → locked M minutes (`SECURITY_CONFIG`); right password still refused | 403 | `USER_LOCKED` |
| Inactive / blocked / must change password / password expired | 403 | `USER_INACTIVE` `USER_BLOCKED` `PASSWORD_CHANGE_REQUIRED` `PASSWORD_EXPIRED` |
| Max active sessions reached (behaviour: REJECT) | 409 | `SESSION_LIMIT_REACHED` |
| Duplicate user name, role without policy, self-deactivate | 409 | `USERNAME_TAKEN` `PASSWORD_POLICY_NOT_CONFIGURED` `CANNOT_DEACTIVATE_SELF` |
| Recovery: wrong answer / too many / expired / invalid token | 401 / 403 / 409 | `SECURITY_ANSWER_INCORRECT` `RESET_ATTEMPTS_EXCEEDED` `RESET_REQUEST_EXPIRED` `RESET_REQUEST_INVALID` |
| Database unreachable / object missing | 500 | `DATABASE_ERROR` |
| Wrong URL / wrong HTTP method / body not JSON / role not allowed | 404 / 405 / 415 / 403 | `ENDPOINT_NOT_FOUND` `METHOD_NOT_ALLOWED` `UNSUPPORTED_MEDIA_TYPE` `FORBIDDEN` |

**Every response has a body.** Actions on a record (activate, unlock …) return the record after the
change. Actions with no record (logout, verify, reset …) return `{ "message": "..." }`. A 500 carries
`detail` (the inner-exception chain), `exceptionType` and stack frames in **every** environment
(owner decision, [ADR 0001](docs/adr/0001-response-bodies-and-routes.md)). Because of this, the API
must stay on the plant or office network. Routes are lowercase with no `-`.
Full guide: [docs/05-user-module-api.md](docs/05-user-module-api.md).

Every change is written to `USER_LOG` (who / what / when / from where / on which record) in the same
transaction as the change; secrets never appear there.

## 5. Not in this cut

Role rights and the `[HasPermission]` attribute — until the roles module lands, user-management and
admin endpoints are gated by the `Administrator` policy (`role_id` claim must equal
`SECURITY_CONFIG.ADMIN_ROLE_ID`, seeded 1). Also: user ↔ plant/excise access mapping, refresh tokens,
rate limiting on the anonymous auth endpoints, a row lock for concurrent logins at the session limit,
the SQL Server provider wiring, Testcontainers, the Line API, the React app, and
`ST.LiquorTNT.Integration` (government portal clients).
