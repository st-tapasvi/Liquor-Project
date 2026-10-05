# API client and contracts

The front end talks only to the ASP.NET Core API under `/api`. The TypeScript view of the API's contracts lives in `src/core/api/contracts/` and mirrors `ST.LiquorTNT.Contracts` class for class. Every API function lives in an `x.api.ts` file and is one thin async function per endpoint on top of the shared `http` instance.

## Today: hand-maintained contracts

The API does not yet publish a committed OpenAPI document, so `contracts/` is maintained by hand. When a backend PR changes a contract, the same change is made here and the backend PR is linked. Type names follow the C# names (`UserResponse`, `LoginRequest`) so the mapping is obvious in review.

Two conventions to keep:

Date-times are IST strings without a zone (`2026-09-28T18:30:00`), typed as the branded `IstDateTime`, displayed as they are (`shared/utils/date.ts`) and never converted to the browser's zone.

Optional text fields are `string | null`, never `undefined`; forms convert `""` to `null` before sending (`emptyToNull`) because the API treats an omitted field on `PUT` as "clear it".

## Next: generated types

When Swagger is exported from the API (`https://localhost:7180/swagger/v1/swagger.json`, saved as `api/openapi.json` in this folder), run:

```bash
npm run api:gen      # npx openapi-typescript@7 api/openapi.json -o src/core/api/generated/schema.d.ts
```

`openapi-typescript` generates types only (no runtime, no fetch client, six direct dependencies), which is why it was preferred over orval. The generated file is committed and is read-only (ESLint ignores it; a CI step can diff a fresh generation against the committed one). At that point each contract in `contracts/` becomes an alias onto the generated schema, for example:

```ts
import type { components } from '../generated/schema';
export type UserResponse = components['schemas']['UserResponse'];
```

and a backend change becomes a TypeScript compile error rather than a runtime surprise. Swashbuckle needs `Nullable` context enabled and `SupportNonNullableReferenceTypes()` for the null-ability of the generated types to be right; check the first generation against `contracts/` before switching.

## Request conventions the client applies

Every request carries `X-Requested-With: XMLHttpRequest` (CSRF, see security.md) and a fresh `X-Correlation-Id`; the API echoes the id and it is shown to users on errors. The session cookie is attached by the browser (`withCredentials: true`); no Authorization header is ever set by the app. Axios's built-in XSRF support echoes the API's readable `XSRF-TOKEN` cookie as `X-XSRF-TOKEN` (`withXSRFToken: true`). Timeouts default to 30 s; a long report or import passes its own `timeout`. Query arrays are serialised as repeated keys. Only 2xx is success; everything else reaches the error interceptor and becomes a typed error (docs/architecture.md, "Errors").

## Per-request flags

`http.get(url, { meta: { … } })` accepts `skipReauth` (this call's own `SESSION_EXPIRED` must not open the dialog — used by the re-authentication login itself) and `silentUnauthorized` (a 401 is a normal answer and must not end the session — used by the start-up `/me` check and by anonymous endpoints that a stale cookie might reach).
