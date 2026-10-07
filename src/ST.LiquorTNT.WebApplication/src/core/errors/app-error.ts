import type { ActiveSessionSummary } from '@/core/api/contracts/auth';

import type { ErrorCode } from './error-codes';

/** Field name → list of messages, exactly as the API sends `errors` on 400 VALIDATION_FAILED. */
export type FieldErrors = Readonly<Record<string, readonly string[]>>;

/**
 * Base class for every error the application raises on purpose.
 * Components never inspect HTTP status codes; they switch on these classes and on `code`.
 */
export class AppError extends Error {
  readonly correlationId: string | undefined;

  constructor(message: string, options?: { cause?: unknown; correlationId?: string | undefined }) {
    super(message, options?.cause === undefined ? undefined : { cause: options.cause });
    this.name = 'AppError';
    this.correlationId = options?.correlationId;
  }
}

/** The API answered with an RFC 7807 problem (4xx/5xx) that is not a field-validation failure. */
export class ApiError extends AppError {
  readonly status: number;
  readonly code: ErrorCode | 'UNKNOWN';
  readonly title: string;
  readonly detail: string | undefined;

  constructor(args: {
    status: number;
    code: ErrorCode | 'UNKNOWN';
    title: string;
    detail?: string | undefined;
    correlationId?: string | undefined;
    cause?: unknown;
  }) {
    super(args.detail ?? args.title, { cause: args.cause, correlationId: args.correlationId });
    this.name = 'ApiError';
    this.status = args.status;
    this.code = args.code;
    this.title = args.title;
    this.detail = args.detail;
  }

  is(code: ErrorCode): boolean {
    return this.code === code;
  }
}

/** 400 VALIDATION_FAILED: the server rejected specific fields. Mapped onto the form by `applyServerErrors`. */
export class ValidationError extends ApiError {
  readonly errors: FieldErrors;

  constructor(args: ConstructorParameters<typeof ApiError>[0] & { errors: FieldErrors }) {
    super(args);
    this.name = 'ValidationError';
    this.errors = args.errors;
  }
}

/**
 * 409 SESSION_LIMIT_REACHED: the account is signed in on as many devices as the server allows.
 *
 * The password was already accepted at this point — the limit is checked after it — so the API sends the
 * account's open sessions with the refusal and the login screen can offer to end one. `canEndOther` is
 * the server's SESSION_FULL_BEHAVIOUR: when it is false the list is shown as information only and the
 * user has to log out on the other device itself.
 */
export class SessionLimitError extends ApiError {
  readonly sessions: readonly ActiveSessionSummary[];
  readonly canEndOther: boolean;

  constructor(
    args: ConstructorParameters<typeof ApiError>[0] & {
      sessions: readonly ActiveSessionSummary[];
      canEndOther: boolean;
    },
  ) {
    super(args);
    this.name = 'SessionLimitError';
    this.sessions = args.sessions;
    this.canEndOther = args.canEndOther;
  }
}

/** No response at all: offline, DNS, timeout, aborted. */
export class NetworkError extends AppError {
  readonly isTimeout: boolean;

  constructor(message: string, options?: { cause?: unknown; isTimeout?: boolean }) {
    super(message, { cause: options?.cause });
    this.name = 'NetworkError';
    this.isTimeout = options?.isTimeout ?? false;
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}

export function isValidationError(error: unknown): error is ValidationError {
  return error instanceof ValidationError;
}

export function isNetworkError(error: unknown): error is NetworkError {
  return error instanceof NetworkError;
}

export function isSessionLimitError(error: unknown): error is SessionLimitError {
  return error instanceof SessionLimitError;
}

/** 4xx errors are the caller's fault: never retried by TanStack Query. */
export function isClientError(error: unknown): boolean {
  return isApiError(error) && error.status >= 400 && error.status < 500;
}

const CHUNK_LOAD_PATTERN =
  /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|ChunkLoadError/i;

/** A lazily loaded screen could not be downloaded (new deployment, dropped connection, dev-server restart). */
export function isChunkLoadError(error: unknown): boolean {
  return error instanceof Error && CHUNK_LOAD_PATTERN.test(`${error.name} ${error.message}`);
}

/** A short, safe message for a snackbar or fallback UI. Never includes stack traces, file names or URLs. */
export function describeError(error: unknown): string {
  if (isValidationError(error)) return 'Some fields need attention.';
  if (isApiError(error)) return error.detail ?? error.title;
  if (isNetworkError(error))
    return error.isTimeout ? 'The server took too long to respond.' : 'The server cannot be reached.';
  if (isChunkLoadError(error))
    return 'This screen could not be loaded. Please reload the page to get the latest version.';
  return 'Something went wrong. Please try again.';
}
