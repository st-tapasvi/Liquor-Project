import type { SupplierCodeResponse } from './access';
import type { IstDateTime } from './common';

/** POST /api/auth/login */
export interface LoginRequest {
  userName: string;
  password: string;
  endSessionId?: number;
}

export interface ActiveSessionSummary {
  id: number;
  loginAt: IstDateTime;
  lastActivityAt: IstDateTime | null;
  expiresAt: IstDateTime;
  ipAddress: string | null;
  userAgent: string | null;
}

export interface CurrentUserResponse {
  userId: number;
  userName: string;
  fullName: string | null;
  companyId: number | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: IstDateTime;
  idleTimeoutMinutes: number;
  user: CurrentUserResponse;
  supplierCodes: SupplierCodeResponse[];
  activeSupplierCode: SupplierCodeResponse | null;
}

export interface SelectSupplierCodeRequest {
  supplierCodeId: number;
}

/** GET /api/auth/mypermissions and the answer of POST /api/auth/selectsuppliercode */
export interface MyPermissionsResponse {
  isSuperAdmin: boolean;
  activeSupplierCode: SupplierCodeResponse | null;
  permissions: string[];
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
