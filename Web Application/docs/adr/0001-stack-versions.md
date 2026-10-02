# 0001 — Stack versions: current mature majors instead of the versions named in the standards document

Date: 2026-09-30 · Status: Accepted

## Context

`frontend-architecture-standards.md` (v1.0, 2026-09-16) fixes React 18, React Router 6 and MUI 6. On npm today (2026-09-30) those lines have had no releases since 2024–2025 and no longer receive security fixes. The project has a 15–20 year horizon and a strict security requirement, which makes "still receives patches" the most important property of a chosen version.

## Decision

Adopt the newest major of each framework that is mature (released for roughly three months with patch releases, and supported by every peer in the dependency list):

| Package      | Chosen                     | Not chosen                                                                                                |
| ------------ | -------------------------- | --------------------------------------------------------------------------------------------------------- |
| React        | 19.2.8                     | 18.3.1 (Apr 2024, no further patches)                                                                     |
| React Router | 7.18.4                     | 6.x (superseded); 8.4 (June 2026, too new, requires Node ≥ 22.22 and React ≥ 19.2.7 — reconsider Q1 2027) |
| MUI          | 9.4.0 / X Data Grid 9.14.0 | 6.x (superseded)                                                                                          |
| Vite         | 8.3.1                      | 7.3.6 (line ended June 2026)                                                                              |
| TypeScript   | 6.0.3                      | 7.0.2 (native compiler, July 2026; typescript-eslint still requires `< 6.1`)                              |
| Vitest       | 4.1.11                     | 5.0.2 (Sept 2026, too new)                                                                                |
| ESLint       | 10.11.0                    | 9.x (maintenance)                                                                                         |

## Consequences

The standards document must be amended (section 2) to name these versions, with the rule "newest mature major" instead of fixed numbers. A few APIs differ from the standards' examples: `react-router` is one package (no `react-router-dom`), MUI 9 uses `slotProps` and `sx` instead of system props on `Stack`, and `eslint-plugin-boundaries` 7 uses `boundaries/dependencies` with `policies`. All are reflected in the scaffold.

## Alternatives considered

Staying on the standards' versions was rejected: it would ship a new codebase on lines that already need a major upgrade for security, front-loading the exact work the standard tries to avoid. Taking every latest major (TS 7, Vitest 5, RR 8) was rejected: weeks-old majors have no track record and, in TypeScript's case, incompatible tooling.
