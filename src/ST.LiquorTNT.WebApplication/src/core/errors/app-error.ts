import type { ActiveSessionSummary } from '@/core/api/contracts/auth';

import type { ErrorCode } from './error-codes';

export type FieldErrors = Readonly<Record<string, readonly string[]>>;


export class AppError extends Error {
  readonly correlationId: string | undefined;

  constructor(message: string, options?: { cause?: unknown; correlationId?: string | undefined }) {
    super(message, options?.cause === undefined ? undefined : { cause: options.cause });
    this.name = 'AppError';
    this.correlationId = options?.correlationId;
  }
}

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

export class ValidationError extends ApiError {
  readonly errors: FieldErrors;

  constructor(args: ConstructorParameters<typeof ApiError>[0] & { errors: FieldErrors }) {
    super(args);
    this.name = 'ValidationError';
    this.errors = args.errors;
  }
}

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

export function isClientError(error: unknown): boolean {
  return isApiError(error) && error.status >= 400 && error.status < 500;
}

const CHUNK_LOAD_PATTERN =
  /Failed to fetch dynamically imported module|error loading dynamically imported module|Importing a module script failed|ChunkLoadError/i;

export function isChunkLoadError(error: unknown): boolean {
  return error instanceof Error && CHUNK_LOAD_PATTERN.test(`${error.name} ${error.message}`);
}

export function describeError(error: unknown): string {
  if (isValidationError(error)) return 'Some fields need attention.';
  if (isApiError(error)) return error.detail ?? error.title;
  if (isNetworkError(error))
    return error.isTimeout ? 'The server took too long to respond.' : 'The server cannot be reached.';
  if (isChunkLoadError(error))
    return 'This screen could not be loaded. Please reload the page to get the latest version.';
  return 'Something went wrong. Please try again.';
}
