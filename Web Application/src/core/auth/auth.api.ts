import type { CurrentUserResponse, LoginRequest, LoginResponse, MessageResponse } from '../api/contracts';
import { http } from '../api/http';

import { tokenStore } from './token.store';

/**
 * Session-level endpoints. They live in core (not features/auth) because the AuthProvider, the
 * re-authentication dialog and the logout menu all need them.
 */
export const authApi = {
  /** Opens a session and keeps its bearer token for every later call. */
  login: async (request: LoginRequest, options?: { forReauth?: boolean }): Promise<LoginResponse> => {
    const { data } = await http.post<LoginResponse>('/auth/login', request, {
      meta: options?.forReauth ? { skipReauth: true, silentUnauthorized: true } : { silentUnauthorized: true },
    });
    tokenStore.set(data.accessToken);
    return data;
  },

  /**
   * Ends this session on the server, then forgets the token. The token is dropped even when the call
   * fails (network down, session already over): a token the server no longer honours is useless here.
   */
  logout: async (): Promise<MessageResponse> => {
    try {
      const { data } = await http.post<MessageResponse>('/auth/logout', undefined, {
        meta: { silentUnauthorized: true },
      });
      return data;
    } finally {
      tokenStore.clear();
    }
  },

  /** Who am I? Used at start-up to restore the session after a page reload. A 401 here is normal. */
  me: async (): Promise<CurrentUserResponse> => {
    const { data } = await http.get<CurrentUserResponse>('/auth/me', {
      meta: { silentUnauthorized: true, skipReauth: true },
    });
    return data;
  },
};
