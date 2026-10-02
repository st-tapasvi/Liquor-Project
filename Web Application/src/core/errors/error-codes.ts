/**
 * Every `errorCode` the API can return (docs/05-user-module-api.md §5).
 * Screen logic is built on these codes, never on message text.
 */
export const ERROR_CODES = [
  'VALIDATION_FAILED',
  'INVALID_CREDENTIALS',
  'UNAUTHENTICATED',
  'SESSION_INVALID',
  'SESSION_TIMED_OUT',
  'SESSION_EXPIRED',
  'SECURITY_ANSWER_INCORRECT',
  'USER_LOCKED',
  'USER_INACTIVE',
  'USER_BLOCKED',
  'PASSWORD_CHANGE_REQUIRED',
  'PASSWORD_EXPIRED',
  'RESET_ATTEMPTS_EXCEEDED',
  'FORBIDDEN',
  'CSRF_REJECTED',
  'NOT_FOUND',
  'USERNAME_TAKEN',
  'PASSWORD_POLICY_NOT_CONFIGURED',
  'CANNOT_DEACTIVATE_SELF',
  'CANNOT_CHANGE_OWN_ROLE',
  'SESSION_LIMIT_REACHED',
  'SECURITY_QUESTION_NOT_SET',
  'SECURITY_QUESTION_DISABLED',
  'RESET_REQUEST_EXPIRED',
  'RESET_REQUEST_INVALID',
  'ENDPOINT_NOT_FOUND',
  'METHOD_NOT_ALLOWED',
  'UNSUPPORTED_MEDIA_TYPE',
  'REQUEST_INVALID',
  'REQUEST_REFUSED',
  'DATABASE_ERROR',
  'UNEXPECTED_ERROR',
] as const;

export type ErrorCode = (typeof ERROR_CODES)[number];

/** Codes that end the session: the app clears its state and shows the login page. */
export const SESSION_ENDED_CODES: readonly ErrorCode[] = ['UNAUTHENTICATED', 'SESSION_INVALID', 'SESSION_TIMED_OUT'];

/** The hard limit: the user re-enters the password in place and the failed call is retried. */
export const SESSION_EXPIRED_CODE: ErrorCode = 'SESSION_EXPIRED';

export function isKnownErrorCode(value: unknown): value is ErrorCode {
  return typeof value === 'string' && (ERROR_CODES as readonly string[]).includes(value);
}
