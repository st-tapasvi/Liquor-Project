# 0003 — Alignment with the Excise Frontend Architecture Specification v1.0

Date: 2026-09-30 · Status: Accepted · Supersedes nothing; extends ADR 0001 (stack versions).

## Context

The _Excise Frontend Architecture Specification v1.0_ (17 September 2026, Dax Padaliya) is the approved
implementation baseline for the merged platform that replaces eleven state-wise desktop applications.

The application built in this repository already matched the document's structural core without having
been written from it: six layers (`app / features / entities / shared / core / test`), the one-way rule
`app → features → entities → shared → core` enforced by `eslint-plugin-boundaries`, the module shape
(`pages/`, `components/`, `api/*.api|queries|mutations.ts`, `routes.tsx`, `index.ts`) and the naming
conventions of §7. What was missing was the multi-excise machinery (§8), tenant isolation (§9), the
`name.keys.ts` convention and sixteen of the seventeen modules of §3.2.

Three items in §2 conflict with decisions the owner had already taken in this repository.

## Decision

The specification governs structure, layering, placement, naming, multi-excise strategy, security
posture and quality gates. Its §2 version pins are superseded by ADR 0001:

| §2              | This repository                                       | Why                                                                                                                                                                                                                                                                                                                                                            |
| --------------- | ----------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| React 18        | React 19                                              | ADR 0001 selected the latest _mature_ majors; React 19 met that bar and React 18 is the older line.                                                                                                                                                                                                                                                            |
| React Router v6 | React Router v7 (library mode, `createBrowserRouter`) | Same rule. v7 keeps the v6 API this codebase uses; per-module lazy loading, which is what §2 asks of the router, is unchanged.                                                                                                                                                                                                                                 |
| Orval           | `openapi-typescript` (`npm run api:gen`)              | Both generate the client from the backend's OpenAPI document, which is what §2 and §14.1 actually require ("generated from the backend OpenAPI specification; never hand-written"). Orval additionally generates hooks; this codebase writes its own `.queries.ts` / `.mutations.ts` so the cache-key factories and invalidation stay explicit and reviewable. |

Everything else in the document is implemented as written. Where the document and the code disagree on
anything not listed above, the document wins.

## Consequences

The version table in §2 of the specification should be read together with this ADR; the deviation is
deliberate, recorded, and confined to three lines. Nothing in §3–§13 depends on the React or Router
major, so the architecture the document defines is unaffected.

Moving to Orval later is contained: it would replace `api:gen` and the hand-written query hooks, and the
`name.keys.ts` factories would move into its generated output. No module code outside `api/` changes.

## Alternatives considered

Downgrading to React 18 and Router v6 to match §2 literally: rewrites the router layer and moves the
product onto an older React for the fifteen-year horizon §1 sets out, in exchange for matching a table
that was written before those versions were evaluated. Rejected by the owner.

Amending the specification instead of recording an ADR: the document is an approved baseline under CTO
review; an ADR is the mechanism §2 itself names for changing a fixed selection.
