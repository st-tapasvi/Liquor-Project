# ADR 0001 — Every response has a body; routes have no `-`

**Status:** accepted (owner), 2026-09-28
**Touches:** standards §4 (route naming) and §5 (response format, diagnosable failures)

## Context

People troubleshoot from what the API returns: testers in Swagger, plant IT, and the React screens.
Three things made that hard:
- `204 No Content` on actions (logout, unlock, reset …) looked in Swagger as if nothing had happened.
- Some errors came back with an empty body: a role refused by the authorization policy (403), no
  matching route (404), the wrong HTTP method (405), a body that was not JSON (415).
- In Production a 500 showed only a title, and the cause sat in the server log.

The owner also asked that route names contain no `-`.

## Decision

1. **Every response has a body.** An action on a record returns the record after the change (for
   example `POST /api/users/{id}/unlock` → `UserResponse`). An action with no record returns
   `MessageResponse` → `{ "message": "Logged out." }`. No `204`.
2. **Every error is ProblemDetails with an `errorCode`**, including framework refusals.
   `ExceptionMiddleware` fills a bare error status after the pipeline (`ENDPOINT_NOT_FOUND`,
   `METHOD_NOT_ALLOWED`, `UNSUPPORTED_MEDIA_TYPE`, `FORBIDDEN`). MVC's own client-error bodies are
   switched off (`SuppressMapClientErrors`), so there is a single error shape.
3. **Exception detail in every environment.** The inner-exception chain (`detail`), `exceptionType`
   and 15 stack frames are returned in Production too.
4. **Routes:** all lowercase with the words joined, no `-`: `/api/auth/changepassword`,
   `/api/auth/forgotpassword/start`, `/api/securityquestions`, `/api/securityconfig`,
   `/api/passwordpolicies`.

## Consequences

- **Security cost (accepted):** a 500 reveals table names, SQL fragments and file paths to whoever
  calls the API. The API must therefore stay on the plant or office network, never exposed to the
  internet. Revisit this decision before any internet-facing deployment.
- `MessageResponse` is a payload, not the forbidden `{ success, data }` envelope. Data-returning
  endpoints still return their data directly.
- Clients using the old hyphenated routes get `404 ENDPOINT_NOT_FOUND`. There are no external
  clients yet, so no aliases are kept.
