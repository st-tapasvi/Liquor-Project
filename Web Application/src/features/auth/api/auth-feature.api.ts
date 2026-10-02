import {
  type ChangePasswordRequest,
  type ForgotPasswordResetRequest,
  type ForgotPasswordStartRequest,
  type ForgotPasswordStartResponse,
  type ForgotPasswordVerifyRequest,
  http,
  type MessageResponse,
  type SecurityQuestionResponse,
  type SessionResponse,
  type SetSecurityQuestionRequest,
} from '@/core/api';

/** Feature-level auth endpoints (login/logout/me live in @/core/auth). One function per endpoint. */
export const authFeatureApi = {
  changePassword: async (request: ChangePasswordRequest): Promise<MessageResponse> => {
    const { data } = await http.post<MessageResponse>('/auth/changepassword', request, {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  forgotPasswordStart: async (request: ForgotPasswordStartRequest): Promise<ForgotPasswordStartResponse> => {
    const { data } = await http.post<ForgotPasswordStartResponse>('/auth/forgotpassword/start', request);
    return data;
  },

  forgotPasswordVerify: async (request: ForgotPasswordVerifyRequest): Promise<MessageResponse> => {
    const { data } = await http.post<MessageResponse>('/auth/forgotpassword/verify', request, {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  forgotPasswordReset: async (request: ForgotPasswordResetRequest): Promise<MessageResponse> => {
    const { data } = await http.post<MessageResponse>('/auth/forgotpassword/reset', request);
    return data;
  },

  securityQuestions: async (): Promise<SecurityQuestionResponse[]> => {
    const { data } = await http.get<SecurityQuestionResponse[]>('/securityquestions');
    return data;
  },

  setMySecurityQuestion: async (request: SetSecurityQuestionRequest): Promise<MessageResponse> => {
    const { data } = await http.put<MessageResponse>('/securityquestions/mine', request, {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  mySessions: async (): Promise<SessionResponse[]> => {
    const { data } = await http.get<SessionResponse[]>('/auth/sessions');
    return data;
  },

  revokeSession: async (id: number): Promise<MessageResponse> => {
    const { data } = await http.delete<MessageResponse>(`/auth/sessions/${encodeURIComponent(id)}`);
    return data;
  },
};
