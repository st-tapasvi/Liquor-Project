# Dependencies

The project is meant to live 15–20 years. Every dependency is a liability with a shelf life, so the list is short, every entry has a job that nothing else in the list already does, and each is open source under a permissive licence (MIT / Apache-2.0 / ISC). MUI X Data Grid is the Community edition; the Pro/Premium editions are commercial and are not used.

## Runtime dependencies

| Package                              | Version         | Job                                           | Notes                                                     |
| ------------------------------------ | --------------- | --------------------------------------------- | --------------------------------------------------------- |
| react, react-dom                     | 19.2.8 (exact)  | UI runtime                                    | 19.x since Dec 2024; the current patch line.              |
| react-router                         | 7.18.4 (exact)  | Routing (library mode, `createBrowserRouter`) | v8 (June 2026) held until it has a few months of patches. |
| @tanstack/react-query                | 5.104.0 (exact) | Server state                                  | v5 since Oct 2023.                                        |
| zustand                              | 5.0.15 (exact)  | Session / UI state                            |                                                           |
| @mui/material, @mui/icons-material   | 9.4.0 (exact)   | UI components                                 | v9 since Apr 2026.                                        |
| @mui/x-data-grid                     | 9.14.0 (exact)  | Data grid (Community)                         | Only imported in `shared/components/data-grid`.           |
| @emotion/react, @emotion/styled      | ^11             | MUI's styling engine (peer)                   |                                                           |
| react-hook-form, @hookform/resolvers | ^7.89, ^5.9     | Forms                                         |                                                           |
| zod                                  | ^4.6            | Schemas: forms and env validation             |                                                           |
| axios                                | ^1.20           | HTTP client with interceptors                 | Only imported in `core/api/http.ts`.                      |
| dayjs                                | ^1.11           | Date parsing/formatting                       | 2 kB; no moment.                                          |

## Development dependencies

| Package                                                                                                                                                                                                                      | Job                                                                                                                                               |
| ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| typescript 6.0.3                                                                                                                                                                                                             | Compiler. 6.0 is the last TypeScript on the JS compiler; TS 7 (native) is adopted when typescript-eslint supports it (today it requires `< 6.1`). |
| vite 8, @vitejs/plugin-react 6                                                                                                                                                                                               | Build and dev server                                                                                                                              |
| vitest 4, @vitest/coverage-v8, jsdom                                                                                                                                                                                         | Unit/component tests. Vitest 5 (Sept 2026) held until it has patches.                                                                             |
| @testing-library/react, /dom, /jest-dom, /user-event                                                                                                                                                                         | Behaviour-level component tests                                                                                                                   |
| msw 2                                                                                                                                                                                                                        | API mocking for tests and mock mode. msw 3 (released two days before this scaffold) held.                                                         |
| @playwright/test                                                                                                                                                                                                             | Browser smoke tests                                                                                                                               |
| eslint 10, typescript-eslint 8, eslint-plugin-react-hooks 7, eslint-plugin-react-refresh, eslint-plugin-import-x, eslint-import-resolver-typescript, eslint-plugin-boundaries 7, eslint-config-prettier, @eslint/js, globals | Linting, architecture boundaries                                                                                                                  |
| prettier 3                                                                                                                                                                                                                   | Formatting                                                                                                                                        |
| husky, lint-staged                                                                                                                                                                                                           | Pre-commit hook                                                                                                                                   |
| @types/react, @types/react-dom, @types/node                                                                                                                                                                                  | Types                                                                                                                                             |

Deliberately not included: `eslint-plugin-jsx-a11y` (last release 2024, no ESLint 10 support), `orval` (25 direct dependencies; `npx openapi-typescript@7` is run on demand instead), `dompurify` (added with the first `SafeHtml` consumer), `i18next` (no multilingual requirement yet), `web-vitals` and a Sentry SDK (on-premise; the logger's remote transport covers it).

## Versioning policy

Framework-level packages (react, react-router, @tanstack/_, @mui/_, zustand, typescript, vite, vitest) are pinned exactly so a `npm install` on a different day cannot change behaviour. Everything else uses `^` within the major. `package-lock.json` is committed and CI installs with `npm ci`, which refuses any drift between `package.json` and the lockfile. Node is pinned by `.nvmrc` (24 LTS) and `engines` (`^22.22 || ^24`); npm 11 is required because npm 10 has an arborist bug with this dependency graph.

## Selecting a version

A major version is adopted when it has been released for roughly three months, has received patch releases, and every peer in this list declares support for it. Newer majors are noted in this file with the date they will be reconsidered. Prefer the newest mature major over the previous one: security fixes stop flowing to old majors sooner than people expect.

## Keeping up to date

Dependabot opens grouped pull requests every Monday for patch and minor updates (`.github/dependabot.yml`). A reviewer checks the changelog for anything security-related, lets CI run the full gate, and merges the same week. Majors are excluded from the bot; they get an upgrade branch, an ADR when the change is architectural (a new router API, a new UI library major), a run of every test level, and an entry in the repository's `VERSIONS.md`.

`npm audit --omit=dev --audit-level=high` runs in CI and fails the build. `npm audit` on dev dependencies is informational (`npm run audit:all`): a vulnerability in a test tool cannot reach production, but it is still fixed within the month. When a fix is unavailable upstream, use an `overrides` entry in `package.json` with a comment and a removal date, never `--force`.

Every quarter, run `npx npm-check-updates` (do not install it permanently) to list outdated majors, and review whether any package in the list above has been deprecated or has stopped releasing. A package with no release for two years is replaced or vendored.

## Adding a dependency

State in the PR what problem it solves, which alternatives were considered (including writing 50 lines yourself), its licence, its weekly download count and release cadence, and whether it has install scripts. The frontend lead approves. Prefer packages with types, no install scripts, few transitive dependencies and more than one maintainer.

## Long-term horizon

React, React Router, TanStack Query and MUI have all shipped compatible upgrades across majors for a decade; the layered structure keeps them replaceable regardless. React is confined to `app/`, `shared/` and the components inside features; the API layer (`x.api.ts`, contracts, error mapping) has no React in it. MUI is confined to `shared/` wrappers, `core/theme` and component files; a UI library change touches those, not the data or routing code. Axios is behind one file. Zustand stores are 40-line modules. Replacing any of these is a bounded task, which is the property that matters over 15 years.
