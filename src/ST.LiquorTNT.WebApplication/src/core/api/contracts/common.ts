/**
 * Hand-maintained TypeScript view of the API contracts (ST.LiquorTNT.Contracts).
 *
 * These files are a stop-gap until `api/openapi.json` is exported from the API's Swagger endpoint and
 * `npm run api:gen` produces `src/core/api/generated/schema.d.ts`. At that point the types below are
 * replaced by aliases onto the generated schema and never hand-edited again (see docs/api-client.md).
 *
 * Naming follows the C# classes 1:1 so the mapping is obvious in code review.
 */

/** `{ "message": "…" }` – every action that has nothing else to return. */
export interface MessageResponse {
  message: string;
}

/** Paged list shape used by every list endpoint. */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

/**
 * Date-times from the API are Indian Standard Time WITHOUT a zone designator ("2026-09-28T18:30:00").
 * They are displayed as-is and never converted. The branded type stops accidental `new Date()` maths.
 */
export type IstDateTime = string & { readonly __brand: 'IstDateTime' };
