# ST.LiquorTNT.WebApplication

React + TypeScript front end of Liquor Track & Trace. It talks to the ASP.NET Core API (`../ST.LiquorTNT.Api`) under `/api`.

Stack: React 19 · TypeScript 6 · Vite 8 · React Router 7 · TanStack Query 5 · Zustand 5 · MUI 9 + MUI X Data Grid · react-hook-form + zod · Axios.

## Run

```bash
npm install
npm run start   # API + Vite together
npm run dev     # Vite only (API already running)
```

## Scripts

| Script                            | What it does                         |
| --------------------------------- | ------------------------------------ |
| `npm run start`                   | API (`dotnet run`) + Vite dev server |
| `npm run dev`                     | Vite dev server with `/api` proxy    |
| `npm run build`                   | Typecheck + production build         |
| `npm run typecheck`               | TypeScript check                     |
| `npm run lint` / `lint:fix`       | ESLint                               |
| `npm run format` / `format:check` | Prettier                             |
| `npm run check`                   | Typecheck + lint + format + audit    |

## Folders

```
src/
├─ app/       providers, router, layout (AppShell, AuthLayout, error pages)
├─ features/  one folder per screen group: auth, company, dashboard, settings, users
├─ shared/    reusable UI: data grid, forms, feedback, hooks, utils
└─ core/      api client, auth, router, config, errors, logging, network, theme
```

Imports flow one way: `app → features → shared → core`. A feature never imports another feature.

## Adding a screen

1. Path in `core/router/paths.ts`, right in `core/auth/permissions.ts`.
2. API calls in `features/<name>/api/`, page in `features/<name>/`.
3. Route with `createProtectedRoute({ path, page, permission })` in the feature's `routes.tsx`.
4. Menu entry in `app/layout/AppShell/menu.config.ts`.
