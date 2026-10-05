import type { AxiosInstance } from 'axios';

import { appConfig } from '../../config';

/**
 * Cross-site request forgery defence, layer 2 (layer 1 is the SameSite=Lax cookie set by the API).
 *
 * Every request carries `X-Requested-With: XMLHttpRequest`. A browser will only add a custom header to
 * a cross-origin request after a successful CORS preflight, and the API's CORS policy does not allow
 * foreign origins, so a forged form post or image request from another site can never carry it. The
 * API rejects state-changing requests that lack the header (403 CSRF_REJECTED).
 */
export function installCsrfInterceptor(http: AxiosInstance): void {
  http.interceptors.request.use((config) => {
    config.headers.set(appConfig.requestedWithHeader, appConfig.requestedWithValue);
    return config;
  });
}
