# 0002 — The JWT travels in an HttpOnly cookie, never in the response body or browser storage

Date: 2026-09-30 · Status: Accepted (frontend implemented; backend change delivered as a patch, see docs/backend-changes.md). Supersedes the same-day draft that used a random session id.

## Context

The API (as of 2026-09-30) returns a signed HS256 JWT in the login body and expects `Authorization: Bearer …`. It already keeps a server-side record of every session (`USER_SESSION`: hash of the token, idle window, hard limit, logout, revocation) and checks it on every request (`SessionValidationMiddleware`), so the JWT alone never decides whether a call is allowed.

Returning the token in the body forces the browser to keep it somewhere JavaScript can read (memory, `sessionStorage`, `localStorage`), which any script running in the page — an XSS payload, a compromised dependency — can exfiltrate. The owner first considered a Frappe-style random session id and then decided to keep the JWT as the only credential type, but delivered to browsers in a cookie.

## Decision

The API keeps issuing exactly the JWT it issues today. For a browser login (`POST /api/auth/login`) it puts the JWT in a cookie named `jwt` with `Path=/; HttpOnly; Secure; SameSite=Lax; Max-Age=<seconds to the hard limit>` and omits it from the body. The bearer handler reads the token from that cookie when no `Authorization` header is present (`JwtBearerEvents.OnMessageReceived`), so claims, the session row check and authorization are identical for both transports. Non-browser clients (the line application, the API tests) use `POST /api/auth/token`, which returns the same JWT in the body for the bearer header and never sets a cookie.

The React app sends every request with `withCredentials` and the `X-Requested-With` header, learns who is logged in from `GET /api/auth/me`, keeps only the user profile and session deadlines in memory, and handles the three session-ending codes as before. The ESLint configuration forbids `localStorage`, `sessionStorage` and `document.cookie` in application code so the invariant cannot erode.

## Consequences

Positive: XSS can no longer steal the token; a page reload keeps the user logged in (the cookie survives, `/me` confirms); the session-expired dialog and the request retry queue work unchanged; one credential format across web and desktop; the backend change is small (a cookie helper, one event handler, the controller, a CSRF middleware). CSRF becomes the relevant risk and is covered by `SameSite=Lax` plus the required custom header and an `Origin` check. HTTPS is mandatory in every environment that is not local development, because `Secure` cookies are not sent over http.

Negative: the JWT is larger than a random id (a few hundred bytes per request); the cookie lifetime and the JWT `exp` must stay aligned (both come from `SESSION_EXPIRY_MINUTES`); Swagger UI needs the CSRF header injected (done in `Program.cs`).

## Alternatives considered

Random session id in the cookie (the earlier draft): equally secure, but a second credential format to maintain next to the desktop app's JWT; rejected by the owner for that reason. Bearer JWT kept in memory only: a reload logs the user out and the token is still readable by script for the life of the page. `sessionStorage`: readable by any script, contrary to the standards document.
