# Architecture

This document explains how the web application is put together and why. The normative coding standard is the project-level `frontend-architecture-standards.md`; this file records what the scaffold implements today and the few places where it deliberately deviates (each with an ADR).

## Principles

The front end is a presentation layer. Business rules, validation that matters, authorization and audit live in the ASP.NET Core API. React renders data, collects input and calls the API. When a business value is being computed in React, it is in the wrong place.

The code is organised by business feature, not by file type. A developer fixing a Users bug opens `src/features/users/` and nothing else. Infrastructure that must exist exactly once (HTTP client, session, routing helpers, logging, theme) lives in `src/core/`; UI that knows nothing about the business lives in `src/shared/`.

Dependencies point one way and the linter enforces it:

```
app  →  features  →  entities  →  shared  →  core
```

| From ↓ / To → | app | features            | entities                          | shared | core |
| ------------- | --- | ------------------- | --------------------------------- | ------ | ---- |
| **app**       | own | via `index.ts` only | via `index.ts` only               | ✅     | ✅   |
| **features**  | ⛔  | own feature only    | via `index.ts` only               | ✅     | ✅   |
| **entities**  | ⛔  | ⛔                  | own entity, others via `index.ts` | ✅     | ✅   |
| **shared**    | ⛔  | ⛔                  | ⛔                                | own    | ✅   |
| **core**      | ⛔  | ⛔                  | ⛔                                | ⛔     | own  |

A violation is an ESLint error (`boundaries/dependencies` in `eslint.config.js`), so code review does not need to catch it. `src/test/**` may import anything.

## The five layers

**`app/`** is the composition root. `AppProviders` nests the providers in a fixed order (ErrorBoundary → Theme → QueryClient → Snackbar → Confirm → Auth). `router.tsx` only assembles the route arrays that features export, under two layouts: `AuthLayout` (anonymous screens) and `AppShell` (sidebar, top bar, session countdown, re-authentication dialog). The sidebar is built from `menu.config.ts`, which references the same `PATHS` and permission keys as the routes, so the menu and the guards can never disagree.

**`features/`** holds vertical slices. Each has `routes.tsx` (declared with `createProtectedRoute` / `createPublicRoute`) and `index.ts` that exports only what other layers legitimately need — normally just the routes. A feature starts flat and gets `api/`, `components/`, `pages/` subfolders when it grows past ~8 files (specification §5.1), which is why thirteen of the modules below are three files each and `auth`, `users` and `settings` are split.

All seventeen modules of specification §3.2 are registered: `auth`, `dashboard`, `company`, `plant`, `users`, `brands`, `batches`, `plans`, `code-pool`, `palette`, `case-data`, `dispatch`, `portal-sync`, `outbox`, `reports`, `settings`, `license`. Six of them — `plant`, `plans`, `code-pool`, `palette`, `dispatch`, `outbox` — exist only where the installation enables them. Screens exist today for `auth`, `dashboard`, `users` and `settings`; the rest render `ModulePlaceholder` until their SOP is confirmed, with their route, flag and rights already real.

**`entities/`** is for business objects two or more features read, select or display (for example a `BrandSelect` that Batches, Case Report and Dispatch all need). It is empty on purpose: the first entity is created when the second consumer appears, not before.

**`shared/`** contains domain-agnostic building blocks: `Form*` fields bound to react-hook-form, `AppDataGrid` (the only place MUI X is imported), feedback components, the snackbar and confirm providers, pure utilities with unit tests. The test for whether something belongs here: does the file know the name of any business concept? If yes, it does not.

**`core/`** is infrastructure. `api/http.ts` is the single Axios instance (only file allowed to import axios). `auth/` owns the session store, the permission model, the `AuthProvider` that restores the session at start-up and the re-authentication coordinator. `router/` owns `PATHS`, the guards and the route helper. `errors/` turns every HTTP failure into typed classes. `logging/` is the redacting logger and the error boundary. `config/` validates `import.meta.env` at start-up so a bad build fails immediately. `modules/` is the registry of the seventeen modules and the feature-flag store. `tenant/` holds the selected company, plant and excise. `screen-config/` fetches and validates backend-supplied field definitions. `platform/` loads the last two at start-up.

## Multi-excise configuration

Specification §8: differences between excises are data, never code. There is no `if (exciseCode === 'UP')` anywhere, and `core/tenant` documents that `exciseCode` is carried for display and configuration lookup only.

Three mechanisms, all in `core/`:

| Mechanism            | Where                                                           | What it varies                                                                                                                                                                                                             |
| -------------------- | --------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Feature flags        | `core/modules` — `GET /api/app/modules`                         | Whether a module exists at all. A disabled module is filtered out of `menu.config.ts` **and** its route resolves to page-not-found via `RequireModule`, so it is unreachable rather than merely hidden (§8.2).             |
| Screen configuration | `core/screen-config` — `GET /api/app/screen-config/{screenKey}` | Which fields a screen has, and whether each is visible, mandatory or read-only. `schemaFromFields` builds the Zod schema from the same data, and `DynamicFormFields` renders it, so one screen serves every excise (§8.1). |
| Dedicated modules    | `features/<module>` + a flag                                    | A genuinely distinct workflow, such as the Chhattisgarh dispatch cycle (§8.2).                                                                                                                                             |

## Tenant isolation

`core/tenant` holds the selected company, plant and excise. `tenant.interceptor.ts` attaches `X-Company-Id` and `X-Plant-Id` to every request in one place, so no screen can forget them or override them — the API validates the scope against the session regardless. Switching company calls `queryClient.clear()`, and every cache key is additionally scoped by company id (`name.keys.ts`), so one company's rows cannot be rendered under another even if the wipe were removed (§9).

## Data access

Server data goes through TanStack Query, and only through it. Every API-backed module has up to three files: `x.api.ts` (plain async functions, one per endpoint, no React), `x.queries.ts` (`useQuery` hooks and the query-key factory) and `x.mutations.ts` (`useMutation` hooks that invalidate through the factory and give user feedback). Components call hooks; they never call `http` or an API function directly.

Query keys live in `name.keys.ts` (specification §7) and start with the object name followed by the company id (`['users', 1, 'list', params]`), so a company switch can never show another tenant's cache. Hooks read the scope with `useCompanyScope()`.

Client state is small and purpose-specific: `session.store.ts` (who is logged in), `reauth.store.ts` (the parked requests during re-authentication). Grid paging and search live in the URL (`useServerGrid`) so a screen is bookmarkable and survives reload. Form state lives in react-hook-form. Nothing is copied from a query result into a store.

## Routing

Every page is lazy-loaded through `createProtectedRoute`, which also wraps it in Suspense, a route-level error boundary (`errorElement`) and the permission guard. Route path strings exist only in `core/router/paths.ts`; components navigate with `PATHS.users.edit(id)`. Post-login redirects go through `safeRedirectPath`, which accepts only same-origin absolute paths.

Guards: `RequireAuth` (AppShell), `RequireAnonymous` (AuthLayout — it also performs the post-login redirect, so the login form never navigates), `RequirePermission` (per route) and `RequireModule` (per route, for flag-controlled modules). `RequireModule` wraps `RequirePermission`: a right is only worth checking once the module exists at all.

## Authentication and authorization

Authentication is a JWT carried in an HttpOnly `jwt` cookie and backed by a server-side session row; the browser code never sees the token. Details, threat model and the API contract are in [security.md](security.md) and [ADR 0002](adr/0002-cookie-session-auth.md).

Authorization in the UI is cosmetic. `permissions` from `GET /api/auth/me` decide what the menu, dashboard tiles, routes (`permission:`) and buttons (`<Can right>`) show. The API enforces every call regardless. The permission keys are a closed union in `core/auth/permissions.ts`; unknown strings from a newer API are ignored.

## Errors

`error.interceptor.ts` converts every Axios failure into `ValidationError` (400 with field errors), `ApiError` (any other ProblemDetails, carrying `code` and `correlationId`) or `NetworkError`. Components switch on these classes and on `error.code`, never on HTTP status. Field errors are mapped onto forms with `applyServerErrors`; everything else is announced by the global mutation handler (snackbar with the correlation reference) or by the route error boundary. Session-ending codes and the hard-limit code are handled in one place, the interceptor.

## Logging

`logger.debug/info/warn/error(message, context)` attaches app version, user id, route and the last correlation id, redacts credential-like keys, and writes to the console in development or to a batched remote transport in production (disabled until `POST /api/client-logs` exists in the API; a one-line switch in `logger.ts`). `console.*` is forbidden everywhere else by ESLint.

## Deviations from the standards document

| Standard says                   | Scaffold does                                                                                     | Why                                                                                                                                                    |
| ------------------------------- | ------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------ |
| React 18, React Router 6, MUI 6 | React 19, React Router 7, MUI 9                                                                   | Those lines no longer receive security patches. [ADR 0001](adr/0001-stack-versions.md), [ADR 0003](adr/0003-excise-frontend-architecture-baseline.md). |
| pnpm                            | npm 11                                                                                            | Owner decision on 2026-09-30; npm 11 has lockfile v3 with integrity hashes and `npm ci`.                                                               |
| orval-generated client          | hand-written contracts now, `openapi-typescript` (types only) when `api/openapi.json` is exported | 25 fewer transitive dev dependencies; [api-client.md](api-client.md).                                                                                  |
| `eslint-plugin-jsx-a11y`        | not included                                                                                      | Last release 2024, no ESLint 10 support. Accessible names are checked in tests and review.                                                             |
| i18n in `core/`                 | not yet                                                                                           | English at launch (specification §14.2); the layer is added when Hindi is approved.                                                                    |

Tenant context, feature flags and screen configuration are now implemented in `core/` as the specification names them. The three endpoints they need do not exist in the API yet — [backend-changes.md](backend-changes.md) specifies them. Until the real API implements them, platform start-up degrades safely but optional modules and tenant-scoped workflows remain unavailable.
