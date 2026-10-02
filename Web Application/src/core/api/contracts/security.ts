import type { IstDateTime } from './common';

/** GET /api/securityconfig */
export interface SecurityConfigResponse {
  key: string;
  value: string;
  dataType: 'INT' | 'BOOL' | 'STRING' | (string & {});
  description: string | null;
  updatedAt: IstDateTime | null;
}

/** PUT /api/securityconfig/{key} */
export interface UpdateSecurityConfigRequest {
  value: string;
}

/** GET /api/passwordpolicies */
export interface PasswordPolicyResponse {
  id: number;
  name: string;
  minLength: number;
  maxLength: number;
  requireUppercase: boolean;
  requireLowercase: boolean;
  requireNumber: boolean;
  requireSpecialCharacter: boolean;
  passwordHistoryCount: number;
  passwordExpiryEnabled: boolean;
  passwordExpiryDays: number | null;
  allowUsernameInPassword: boolean;
  allowCommonPassword: boolean;
}

/** PUT /api/passwordpolicies/{id} – send ALL fields. */
export type UpdatePasswordPolicyRequest = Omit<PasswordPolicyResponse, 'id' | 'name'>;
