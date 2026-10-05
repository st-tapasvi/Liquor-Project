import type { AxiosInstance } from 'axios';

import { tokenStore } from '../../auth/token.store';

/**
 * Sends the session's bearer token on every request: `Authorization: Bearer <accessToken>`.
 *
 * The token comes from POST /api/auth/login and lives in `core/auth/token.store`. Anonymous calls
 * (login, forgot password) simply have no token yet, so nothing is sent. A request that already set
 * its own Authorization header (none today) is left alone.
 */
export function installAuthInterceptor(http: AxiosInstance): void {
  http.interceptors.request.use((config) => {
    if (!config.headers.has('Authorization')) {
      const token = tokenStore.get();
      if (token !== null) config.headers.set('Authorization', `Bearer ${token}`);
    }
    return config;
  });
}
