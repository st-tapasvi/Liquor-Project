# Backend changes required by the web application

The React app assumes four things from `ST.LiquorTNT.Api` that the API (as of 2026-09-30) does not do yet. The code for all four was written directly into `.Net Application/src` and `tests` on 2026-09-30 (15 files, listed below). It was written against the current source but **could not be compiled where it was written** (no .NET SDK there), so the first thing to do is `dotnet build ST.LiquorTNT.sln` and `dotnet test ST.LiquorTNT.sln`, and review the diff in git.

| #   | Change                                                                                                                                                                          | Why the web app needs it                                                |
| --- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------- |
| 1   | Browser login puts the JWT in an HttpOnly cookie named `jwt` (`Path=/; HttpOnly; Secure; SameSite=Lax`) instead of the response body; the bearer handler also reads that cookie | The app stores no credential anywhere script can reach (ADR 0002)       |
| 2   | `GET /api/auth/me` and the login response include `isAdministrator` and `permissions[]`                                                                                         | Menu, tiles, routes and buttons are driven by rights, never by role ids |
| 3   | CSRF middleware: `Origin` check on writes, `X-Requested-With` required when the cookie is used; new error code `CSRF_REJECTED`                                                  | Cookie-carried credentials need CSRF defence in depth                   |
| 4   | The API serves the built app from `wwwroot` at `/` with an SPA fallback, cache headers and security headers (CSP, HSTS, …)                                                      | One installation, one origin; deep links reload; browser hardening      |

## Files in the patch

| File                                          | Status   | Summary                                                                                                     |
| --------------------------------------------- | -------- | ----------------------------------------------------------------------------------------------------------- |
| `Contracts/Auth/LoginResponse.cs`             | modified | `AccessToken` becomes nullable and is omitted from JSON when null (browser login)                           |
| `Contracts/Auth/CurrentUserResponse.cs`       | modified | adds `IsAdministrator`, `Permissions`                                                                       |
| `Business/Auth/AuthService.cs`                | modified | unchanged login flow; `ToCurrentUser` fills the rights from `SECURITY_CONFIG.ADMIN_ROLE_ID`                 |
| `Business/Auth/WebPermissions.cs`             | new      | the permission keys (mirror of `core/auth/permissions.ts`)                                                  |
| `Business/Common/ErrorCodes.cs`               | modified | adds `CsrfRejected`                                                                                         |
| `Business/Common/IRequestContext.cs`          | modified | comment only: `AccessToken` is "jwt cookie or bearer header"                                                |
| `Api/Security/JwtCookie.cs`                   | new      | cookie name, attributes, write/read/delete                                                                  |
| `Api/Security/RequestContext.cs`              | modified | `AccessToken` reads the `jwt` cookie first, then the bearer header                                          |
| `Api/Middleware/CsrfProtectionMiddleware.cs`  | new      | Origin + `X-Requested-With` rules                                                                           |
| `Api/Middleware/SecurityHeadersMiddleware.cs` | new      | CSP and the other headers                                                                                   |
| `Api/Controllers/AuthController.cs`           | modified | `login` sets the cookie and blanks the body token; new `token` endpoint; `logout` clears the cookie         |
| `Api/Extensions/ApiServiceExtensions.cs`      | modified | `JwtBearerEvents.OnMessageReceived` takes the token from the cookie; CORS `AllowCredentials`; Swagger note  |
| `Api/Program.cs`                              | modified | forwarded headers, security headers, static files with cache rules, CSRF, SPA fallback, Swagger CSRF header |
| `tests/Api.Tests/ApiTestHelpers.cs`           | modified | `LoginOk`/`LoginFail` call `/api/auth/token` (the bearer path the existing tests drive)                     |
| `tests/Api.Tests/JwtCookieTests.cs`           | new      | cookie login, no token in body, CSRF refusal, logout clears cookie, token login still works                 |

Nothing in the database changes and nothing in the session logic changes: the JWT is the same, `USER_SESSION.SESSION_TOKEN_HASH` stores the same hash, and `SessionValidationMiddleware`, `SessionService`, `ISessionRepository`, `JwtAccessTokenService` and every Business test are untouched. `IRequestContext.AccessToken` simply returns the cookie value for browser requests.

## How to verify

```powershell
cd ".Net Application"
git status                            # the 15 changed/new files
dotnet build ST.LiquorTNT.sln
dotnet test  ST.LiquorTNT.sln          # Api and Infrastructure tests need the dev MySQL
```

Then copy the web build into `src/ST.LiquorTNT.Api/wwwroot` (see [deployment.md](deployment.md)), run the API, and open `https://localhost:7180/`: the login page is served by the API, and the login sets the `jwt` cookie.

## The contract the web app relies on

`POST /api/auth/login` with `{ userName, password }` and the header `X-Requested-With: XMLHttpRequest` answers `200` with `{ expiresAt, idleTimeoutMinutes, user }` and `Set-Cookie: jwt=<token>; Path=/; HttpOnly; Secure; SameSite=Lax; Max-Age=<seconds to the hard limit>`. Every error keeps its existing code (`INVALID_CREDENTIALS`, `USER_LOCKED`, `PASSWORD_CHANGE_REQUIRED`, `SESSION_LIMIT_REACHED`, …).

When the account is already on `MAX_ACTIVE_SESSIONS` devices the answer is `409 SESSION_LIMIT_REACHED`, and the problem body carries two extension members so the login screen can offer a way out:

```jsonc
{
  "title": "Maximum active sessions reached.",
  "status": 409,
  "errorCode": "SESSION_LIMIT_REACHED",
  "canEndOtherSession": true, // SECURITY_CONFIG.SESSION_FULL_BEHAVIOUR == REJECT_ALLOW_EVICT
  "sessions": [
    {
      "id": 71,
      "loginAt": "2026-09-28T08:00:00",
      "lastActivityAt": "2026-09-28T09:30:00",
      "expiresAt": "2026-09-29T08:00:00",
      "ipAddress": "192.168.1.20",
      "userAgent": "Mozilla/5.0 …",
    },
  ],
}
```

Re-sending the login with `{ userName, password, endSessionId: 71 }` revokes that session — only if it belongs to the account being logged into, and only while the setting allows it — and then opens the new one. An id that is not the user's, or is already over, is ignored rather than refused, so the endpoint cannot be used to probe which session ids exist. Migration `db/mysql/008_session_full_behaviour.sql` moves `SESSION_FULL_BEHAVIOUR` to `REJECT_ALLOW_EVICT`; the code default stays `REJECT`.

`GET /api/auth/me` answers the user with `isAdministrator` and `permissions` while the cookie's session is alive; `401 UNAUTHENTICATED` when there is no cookie or the token is invalid; `401 SESSION_TIMED_OUT` / `SESSION_EXPIRED` / `SESSION_INVALID` exactly as today when the session has ended (an expired JWT in the cookie is `SESSION_EXPIRED`, like an expired bearer token).

`POST /api/auth/logout` ends the session and answers with `Set-Cookie: jwt=; expires=<past>`.

State-changing requests that carry the cookie without `X-Requested-With: XMLHttpRequest`, or any write whose `Origin` is not this host or an allowed dev origin, get `403 CSRF_REJECTED`.

`POST /api/auth/token` is the unchanged bearer flow for the line application and for the existing end-to-end tests; it never sets a cookie.

## Headers the API sends for the web app

```
Content-Security-Policy: default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';
  img-src 'self' data:; font-src 'self' data:; connect-src 'self'; object-src 'none';
  base-uri 'self'; form-action 'self'; frame-ancestors 'none'; upgrade-insecure-requests
Strict-Transport-Security: max-age=31536000; includeSubDomains        (https only)
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: strict-origin-when-cross-origin
Permissions-Policy: camera=(), microphone=(), geolocation=(), payment=(), usb=()
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Resource-Policy: same-origin
Cache-Control: public, max-age=31536000, immutable                    (/assets/*)
Cache-Control: no-store, no-cache                                     (index.html and other files)
```

`style-src 'unsafe-inline'` is needed because MUI's Emotion runtime injects `<style>` elements. Scripts remain strictly same-origin with no inline execution, which is the part that stops XSS payloads from running. Moving styles to a per-request nonce (Emotion supports a `nonce` on its cache) is the planned follow-up once the API can inject a nonce into `index.html`.

## Review notes to check when applying

The earlier draft of this patch was reviewed line by line against the current source; the JWT-cookie version is smaller and reuses the reviewed pieces. Items to check when applying: the desktop application, if it reads `LoginResponse.AccessToken`, must switch to `POST /api/auth/token` and treat the value as nullable; a reverse proxy in front of the API must forward `X-Forwarded-Host` (or the public origin must be listed in `Cors:AllowedOrigins`) or the CSRF `Origin` check will refuse browser writes; and `Jwt:SigningKey` must be changed on every installation, as `appsettings.json` already says, because the cookie now makes the JWT the browser's only credential.

## Platform endpoints required by the merged architecture

Three endpoints make one application serve eleven excises (Frontend Architecture Specification §8, §9). None exist yet; the React app calls the real API at start-up and degrades safely when they fail — every optional module stays hidden and no tenant headers are sent. They must be implemented before tenant-scoped workflows can be used.

| Endpoint                                 | Answers                                                                                                                                        | Used for                                                                                                                                    |
| ---------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| `GET /api/app/modules`                   | `{ "enabled": ["plant", "dispatch", "outbox"] }`                                                                                               | Which flagged modules this installation has. Read from the database, per installation.                                                      |
| `GET /api/app/tenant`                    | `{ companies: [{ companyId, companyName, exciseCode }], plants: [{ plantId, plantName, companyId }], defaultCompanyId, defaultPlantId }`       | The companies and plants the signed-in user may work in.                                                                                    |
| `GET /api/app/screen-config/{screenKey}` | `{ screenKey, exciseCode, updatedAt, fields: [{ name, label, kind, visible, required, readOnly, sequence, maxLength, helperText, options }] }` | The field definitions of one screen for this company's excise. `kind` is one of `text`, `number`, `date`, `select`, `checkbox`, `textarea`. |

All three require an authenticated session. `screen-config` reads the excise from the session and the `X-Company-Id` header, never from a query parameter the browser controls.

Every request the app sends now carries `X-Company-Id` and, when a plant is selected, `X-Plant-Id`. These are a scope hint, not an authorisation claim: **the API must validate them against the session and refuse a company the user does not belong to.** A browser can send any value.

The six flagged modules are `plant`, `plans`, `code-pool`, `palette`, `dispatch`, `outbox`. A key the API returns that this build does not know is ignored, so the API may add modules ahead of the frontend.

## Follow-ups (not in this patch)

`POST /api/client-logs` to receive batched browser log entries (the web logger's remote transport is written and disabled). Rate-limiting on `login`, `forgotpassword/*` and `changepassword` (`Microsoft.AspNetCore.RateLimiting`, fixed window per IP) as a complement to the account lock. Swashbuckle `SupportNonNullableReferenceTypes()` so the generated TypeScript types carry the right null-ability. Migrating the line application from `token` to the cookie flow is not required; both paths share the same session rules.
