import {
  http,
  type PasswordPolicyResponse,
  type SecurityConfigResponse,
  type UpdatePasswordPolicyRequest,
  type UpdateSecurityConfigRequest,
} from '@/core/api';

export const settingsApi = {
  securityConfig: async (): Promise<SecurityConfigResponse[]> => {
    const { data } = await http.get<SecurityConfigResponse[]>('/securityconfig');
    return data;
  },
  updateSecurityConfig: async (key: string, request: UpdateSecurityConfigRequest): Promise<SecurityConfigResponse> => {
    const { data } = await http.put<SecurityConfigResponse>(`/securityconfig/${encodeURIComponent(key)}`, request);
    return data;
  },
  passwordPolicies: async (): Promise<PasswordPolicyResponse[]> => {
    const { data } = await http.get<PasswordPolicyResponse[]>('/passwordpolicies');
    return data;
  },
  updatePasswordPolicy: async (id: number, request: UpdatePasswordPolicyRequest): Promise<PasswordPolicyResponse> => {
    const { data } = await http.put<PasswordPolicyResponse>(`/passwordpolicies/${encodeURIComponent(id)}`, request);
    return data;
  },
};
