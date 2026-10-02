# ST.LiquorTNT — Web Application

React + TypeScript front end of the Liquor Track & Trace platform. It is served by the ASP.NET Core Web API (`.Net Application/src/ST.LiquorTNT.Api`) at the root URL, talks to that API under `/api`, and is authenticated by a JWT carried in an HttpOnly `jwt` cookie (backed by a server-side session row).

|         |                                                                                                                                                                                                                                                           |
| ------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Stack   | React 19 · TypeScript 6 · Vite 8 · React Router 7 · TanStack Query 5 · Zustand 5 · MUI 9 + MUI X Data Grid (Community, MIT) · react-hook-form + zod 4 · Axios · dayjs                                                                                     |
| Quality | ESLint 10 (type-aware, architecture boundaries) · Prettier · Vitest 4 + Testing Library · Playwright                                                                                                                                                      |
| Node    | 24 LTS (`.nvmrc`); npm ≥ 11                                                                                                                                                                                                                               |
| Docs    | [Architecture](docs/architecture.md) · [Security](docs/security.md) · [Dependencies](docs/dependencies.md) · [API client](docs/api-client.md) · [Deployment](docs/deployment.md) · [Backend changes required](docs/backend-changes.md) · [ADRs](docs/adr) |

## Getting started

```bash
# once per machine
nvm use                 # Node 24 (or install it from nodejs.org)
npm install -g npm@11   # only if `npm -v` is below 11

# once per clone
npm ci                  # exact versions from package-lock.json; install scripts are disabled (.npmrc)
npm run prepare         # installs the git pre-commit hook (husky)

# every day
npm run dev             # http://localhost:5173, proxies /api to the API (VITE_DEV_API_TARGET)
```

The API must be running locally (`dotnet run` in `ST.LiquorTNT.Api`, default `http://localhost:5180`). The web application always uses the real API; there is no in-browser mock backend.

## Scripts

| Script                                      | What it does                                                          |
| ------------------------------------------- | --------------------------------------------------------------------- |
| `npm run dev`                               | Vite dev server with API proxy                                        |
| `npm run build`                             | Typecheck + production build into `dist/` (hidden source maps)        |
| `npm run preview`                           | Serve `dist/` locally                                                 |
| `npm run typecheck`                         | `tsc --noEmit` for the app and the node-side config                   |
| `npm run lint` / `lint:fix`                 | ESLint with `--max-warnings 0`                                        |
| `npm run format` / `format:check`           | Prettier                                                              |
| `npm test` / `test:watch` / `test:coverage` | Vitest (jsdom)                                                        |
| `npm run test:e2e`                          | Playwright smoke flows against a real local API or `E2E_BASE_URL`     |
| `npm run audit`                             | `npm audit` on production dependencies, fails on high/critical        |
| `npm run check`                             | Everything CI runs, in order                                          |
| `npm run api:gen`                           | Regenerate API types from `api/openapi.json` (see docs/api-client.md) |

## Layout

```
src/
├─ app/        composition root: providers, router assembly, AppShell / AuthLayout, error pages
├─ features/   the 17 business modules of the architecture specification — screens + their API calls
├─ entities/   business objects shared by 2+ features (empty until a second consumer appears)
├─ shared/     domain-agnostic UI: forms, data grid, feedback, hooks, utils
├─ core/       infrastructure that exists once: api/http, auth, router, config, errors, logging, theme,
│              modules (registry + feature flags), tenant, screen-config, platform bootstrap
└─ test/       Vitest setup, renderWithProviders and factories
```

Dependencies flow one way, `app → features → entities → shared → core`, and a feature never imports another feature. ESLint enforces it (`boundaries/dependencies`); see [docs/architecture.md](docs/architecture.md) for the rules and the decision table for where a file goes.

## Where things are

| Concern                                                     | Location                                             |
| ----------------------------------------------------------- | ---------------------------------------------------- |
| HTTP client, CSRF/correlation/error interceptors            | `src/core/api/http.ts`, `src/core/api/interceptors/` |
| Session state, permissions, `<Can>`, re-authentication      | `src/core/auth/`                                     |
| Route paths, guards, `createProtectedRoute`                 | `src/core/router/`                                   |
| Error classes and ProblemDetails mapping                    | `src/core/errors/`                                   |
| Logger (redacting), error boundary                          | `src/core/logging/`                                  |
| Environment validation (`VITE_*`)                           | `src/core/config/env.ts`                             |
| API contracts (TypeScript view of `ST.LiquorTNT.Contracts`) | `src/core/api/contracts/`                            |
| Sidebar menu (module- and rights-filtered)                  | `src/app/layout/AppShell/menu.config.ts`             |
| Module registry and feature flags                           | `src/core/modules/`                                  |
| Company / plant / excise context                            | `src/core/tenant/`                                   |
| Backend-driven screen configuration                         | `src/core/screen-config/`                            |

## Adding a feature

1. `src/features/<name>/` with `routes.tsx` and `index.ts` (export only the routes).
2. Paths in `core/router/paths.ts`; rights in `core/auth/permissions.ts` (and in the API).
3. `x.keys.ts` (company-scoped cache keys) → `x.api.ts` → `x.queries.ts` → `x.mutations.ts` (invalidate through the factory).
4. Pages under `pages/`, `export default`, declared with `createProtectedRoute({ permission, module })` — `module` is required for a flag-controlled module.
5. Menu entry in `menu.config.ts` with the same `module` and right; add page tests and real-API E2E coverage where appropriate; wire the routes in `app/router/router.tsx`.
6. A module the specification lists as optional also needs its key in `core/modules/modules.registry.ts` with `availability: 'flag'`, and the API must return that key from `GET /api/app/modules`.
