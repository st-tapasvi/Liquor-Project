import type { IstDateTime } from './common';

/** POST /api/auth/login */
export interface LoginRequest {
  userName: string;
  password: string;
  /**
   * One of this account's own open sessions to end before signing in, so the login fits inside the
   * device limit. Sent only after a 409 SESSION_LIMIT_REACHED, with an id taken from that response.
   * The API honours it only while SESSION_FULL_BEHAVIOUR is REJECT_ALLOW_EVICT.
   */
  endSessionId?: number;
}

/**
 * One open session, as listed in the body of a 409 SESSION_LIMIT_REACHED so the login screen can offer
 * to end one. Same shape as {@link SessionResponse} minus `isCurrent`, which has no meaning before a
 * session exists on this device.
 */
export interface ActiveSessionSummary {
  id: number;
  loginAt: IstDateTime;
  lastActivityAt: IstDateTime | null;
  expiresAt: IstDateTime;
  ipAddress: string | null;
  userAgent: string | null;
}

/**
 * The logged-in user, as returned by login and by GET /api/auth/me.
 *
 * `permissions` is the list of rights the API grants this user (closed keys, see core/auth/permissions.ts).
 * The UI only uses it to show or hide; the API enforces every call regardless.
 */
export interface CurrentUserResponse {
  userId: number;
  userName: string;
  fullName: string | null;
  roleId: number | null;
  companyId: number | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
  isAdministrator: boolean;
  permissions: string[];
}

/**
 * Login answer for the browser. The token itself is NOT in the body: the API sets the
 * `jwt` cookie (HttpOnly, Secure, SameSite=Lax) which the browser attaches automatically.
 */
export interface LoginResponse {
  /** Hard limit of this session (IST). After it every call answers 401 SESSION_EXPIRED. */
  expiresAt: IstDateTime;
  /** Minutes without any API call after which the session ends with 401 SESSION_TIMED_OUT. */
  idleTimeoutMinutes: number;
  user: CurrentUserResponse;
}

/** POST /api/auth/changepassword (public: also the forced first-login change) */
export interface ChangePasswordRequest {
  userName: string;
  currentPassword: string;
  newPassword: string;
}

/** GET /api/auth/sessions */
export interface SessionResponse {
  id: number;
  loginAt: IstDateTime;
  lastActivityAt: IstDateTime | null;
  expiresAt: IstDateTime;
  ipAddress: string | null;
  userAgent: string | null;
  isCurrent: boolean;
}

/** GET /api/securityquestions */
export interface SecurityQuestionResponse {
  id: number;
  questionText: string;
}

/** PUT /api/securityquestions/mine */
export interface SetSecurityQuestionRequest {
  questionId: number;
  answer: string;
  currentPassword: string;
}

/** POST /api/auth/forgotpassword/start */
export interface ForgotPasswordStartRequest {
  userName: string;
}
export interface ForgotPasswordStartResponse {
  requestToken: string;
  questionText: string;
  expiresAt: IstDateTime;
}

/** POST /api/auth/forgotpassword/verify */
export interface ForgotPasswordVerifyRequest {
  requestToken: string;
  answer: string;
}

/** POST /api/auth/forgotpassword/reset */
export interface ForgotPasswordResetRequest {
  requestToken: string;
  newPassword: string;
}
