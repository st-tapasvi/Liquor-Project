import { HttpResponse } from 'msw';

import type { ErrorCode } from '@/core/errors';

/** Builds an error body exactly like the API's ExceptionMiddleware. */
export function problem(
  status: number,
  errorCode: ErrorCode,
  title: string,
  extra?: {
    detail?: string;
    errors?: Record<string, string[]>;
    /** RFC 7807 extension members, as an exception type may add them (e.g. SESSION_LIMIT_REACHED). */
    extensions?: Record<string, unknown>;
  },
) {
  return HttpResponse.json(
    {
      type: `https://errors.stliquortnt.local/${errorCode.toLowerCase()}`,
      title,
      status,
      detail: extra?.detail ?? null,
      instance: '/api/test',
      errorCode,
      correlationId: 'test-correlation-id',
      ...(extra?.errors ? { errors: extra.errors } : {}),
      ...(extra?.extensions ?? {}),
    },
    { status, headers: { 'Content-Type': 'application/problem+json', 'X-Correlation-Id': 'test-correlation-id' } },
  );
}
