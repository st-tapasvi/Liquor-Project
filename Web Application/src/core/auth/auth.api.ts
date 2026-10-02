import type { CurrentUserResponse, LoginRequest, LoginResponse, MessageResponse } from '../api/contracts';
import { http } from '../api/http';

/**
 * Session-level endpoints. They live in core (not features/auth) because the AuthProvider, the
 * re-authentication dialog and the logout menu all need them.
 */
export const authApi = {
  /** Opens a session. The API answers with Set-Cookie: jwt=… (HttpOnly); the body carries no token. */
  login: async (request: LoginRequest, options?: { forReauth?: boolean }): Promise<LoginResponse> => {
    const { data } = await http.post<LoginResponse>('/auth/login', request, {
      meta: options?.forReauth ? { skipReauth: true, silentUnauthorized: true } : { silentUnauthorized: true },
    });
    return data;
  },

  /** Ends this session on the server and clears the cookie. */
  logout: async (): Promise<MessageResponse> => {
    const { data } = await http.post<MessageResponse>('/auth/logout', undefined, {
      meta: { silentUnauthorized: true },
    });
    return data;
  },

  /** Who am I? Used at start-up to restore the session after a page reload. A 401 here is normal. */
  me: async (): Promise<CurrentUserResponse> => {
    const { data } = await http.get<CurrentUserResponse>('/auth/me', {
      meta: { silentUnauthorized: true, skipReauth: true },
    });
    return data;
  },
};
