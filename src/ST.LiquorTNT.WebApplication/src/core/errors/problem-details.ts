import type { ActiveSessionSummary } from '@/core/api/contracts/auth';
import type { IstDateTime } from '@/core/api/contracts/common';

import { ApiError, type FieldErrors, SessionLimitError, ValidationError } from './app-error';
import { isKnownErrorCode } from './error-codes';

export interface ProblemDetails {
  status?: number;
  title?: string;
  detail?: string | null;
  errorCode?: string;
  correlationId?: string | null;
  errors?: Record<string, string[]>;
  sessions?: ActiveSessionSummary[];
  canEndOtherSession?: boolean;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

export function parseProblemDetails(body: unknown): ProblemDetails | undefined {
  if (!isRecord(body)) return undefined;

  const errorsRaw = body['errors'];
  let errors: Record<string, string[]> | undefined;
  if (isRecord(errorsRaw)) {
    errors = {};
    for (const [field, messages] of Object.entries(errorsRaw)) {
      if (Array.isArray(messages)) {
        errors[field] = messages.filter((m): m is string => typeof m === 'string');
      }
    }
  }

  const sessions = parseSessions(body['sessions']);

  return {
    ...(typeof body['status'] === 'number' ? { status: body['status'] } : {}),
    ...(typeof body['title'] === 'string' ? { title: body['title'] } : {}),
    ...(typeof body['detail'] === 'string' ? { detail: body['detail'] } : {}),
    ...(typeof body['errorCode'] === 'string' ? { errorCode: body['errorCode'] } : {}),
    ...(typeof body['correlationId'] === 'string' ? { correlationId: body['correlationId'] } : {}),
    ...(errors ? { errors } : {}),
    ...(sessions ? { sessions } : {}),
    ...(typeof body['canEndOtherSession'] === 'boolean' ? { canEndOtherSession: body['canEndOtherSession'] } : {}),
  };
}


function parseSessions(raw: unknown): ActiveSessionSummary[] | undefined {
  if (!Array.isArray(raw)) return undefined;

  const parsed: ActiveSessionSummary[] = [];
  for (const item of raw) {
    if (!isRecord(item)) continue;
    if (typeof item['id'] !== 'number' || typeof item['loginAt'] !== 'string') continue;

    const ist = (value: unknown): IstDateTime | null => (typeof value === 'string' ? (value as IstDateTime) : null);
    const loginAt = item['loginAt'] as IstDateTime;

    parsed.push({
      id: item['id'],
      loginAt,
      lastActivityAt: ist(item['lastActivityAt']),
      expiresAt: ist(item['expiresAt']) ?? loginAt,
      ipAddress: typeof item['ipAddress'] === 'string' ? item['ipAddress'] : null,
      userAgent: typeof item['userAgent'] === 'string' ? item['userAgent'] : null,
    });
  }

  return parsed;
}

export function toApiError(status: number, body: unknown, fallbackCorrelationId?: string, cause?: unknown): ApiError {
  const problem = parseProblemDetails(body);
  const code = isKnownErrorCode(problem?.errorCode) ? problem.errorCode : 'UNKNOWN';
  const correlationId = problem?.correlationId ?? fallbackCorrelationId;
  const title = problem?.title ?? defaultTitle(status);
  const detail = problem?.detail ?? undefined;

  if (code === 'VALIDATION_FAILED' && problem?.errors) {
    const errors: FieldErrors = problem.errors;
    return new ValidationError({ status, code, title, detail, correlationId, errors, cause });
  }

  if (code === 'SESSION_LIMIT_REACHED') {
    return new SessionLimitError({
      status,
      code,
      title,
      detail,
      correlationId,
      cause,
      sessions: problem?.sessions ?? [],
      canEndOther: problem?.canEndOtherSession ?? false,
    });
  }

  return new ApiError({ status, code, title, detail, correlationId, cause });
}

function defaultTitle(status: number): string {
  if (status === 401) return 'You need to log in again.';
  if (status === 403)
    return `The server refused this action (HTTP 403). If you think this is wrong, contact your administrator.`;
  if (status === 404) return 'What you asked for was not found (HTTP 404).';
  if (status === 429) return 'Too many requests. Wait a moment and try again.';
  if (status >= 500) return `The server could not complete the request (HTTP ${status}). Try again in a moment.`;
  return `The server refused the request (HTTP ${status}).`;
}
