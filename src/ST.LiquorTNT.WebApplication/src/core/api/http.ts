import axios, { type AxiosInstance } from 'axios';

import { appConfig, env } from '../config';

import {
  installCorrelationInterceptor,
  installCsrfInterceptor,
  installErrorInterceptor,
  installTenantInterceptor,
} from './interceptors';

/**
 * The single HTTP client of the application. Only this file imports axios (enforced by ESLint).
 *
 * Authentication is the JWT the API puts in the HttpOnly `jwt` cookie at login (backed by a server-side
 * USER_SESSION row). This code never sees it: the browser attaches it on its own (`withCredentials`), so
 * every tab of the browser shares one session, like ERPNext's `sid`. No Authorization header is set here.
 *
 * CSRF: the API also sets a readable `XSRF-TOKEN` cookie; axios's built-in support echoes it as the
 * `X-XSRF-TOKEN` header, which the API requires on cookie-authenticated writes. `withXSRFToken: true`
 * sends it even if VITE_API_BASE_URL ever points at another origin (the API's own, listed in its CORS).
 */
function createHttp(): AxiosInstance {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: appConfig.requestTimeoutMs,
    withCredentials: true,
    xsrfCookieName: appConfig.csrfCookieName,
    xsrfHeaderName: appConfig.csrfHeaderName,
    withXSRFToken: true,
    headers: { Accept: 'application/json' },
    // Query strings: arrays as repeated keys (`?id=1&id=2`), undefined values dropped.
    paramsSerializer: { indexes: null },
    // Only 2xx is success; everything else goes through the error interceptor.
    validateStatus: (status) => status >= 200 && status < 300,
  });

  // Order matters: request interceptors run last-registered-first, response interceptors in order.
  installCsrfInterceptor(instance);
  installCorrelationInterceptor(instance);
  installTenantInterceptor(instance);
  installErrorInterceptor(instance);

  return instance;
}

export const http: AxiosInstance = createHttp();
