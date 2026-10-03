import axios, { type AxiosInstance } from 'axios';

import { appConfig, env } from '../config';

import {
  installAuthInterceptor,
  installCorrelationInterceptor,
  installCsrfInterceptor,
  installErrorInterceptor,
  installTenantInterceptor,
} from './interceptors';

/**
 * The single HTTP client of the application. Only this file imports axios (enforced by ESLint).
 *
 * Authentication is the bearer JWT the API returns from POST /api/auth/login (backed by a server-side
 * USER_SESSION row). `installAuthInterceptor` adds it as `Authorization: Bearer …` on every call; the
 * token itself lives only in core/auth/token.store. There is no refresh token: the session slides on
 * the server while the user is active and ends with 401 SESSION_TIMED_OUT / SESSION_EXPIRED.
 */
function createHttp(): AxiosInstance {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: appConfig.requestTimeoutMs,
    headers: { Accept: 'application/json' },
    // Query strings: arrays as repeated keys (`?id=1&id=2`), undefined values dropped.
    paramsSerializer: { indexes: null },
    // Only 2xx is success; everything else goes through the error interceptor.
    validateStatus: (status) => status >= 200 && status < 300,
  });

  // Order matters: request interceptors run last-registered-first, response interceptors in order.
  installCsrfInterceptor(instance);
  installAuthInterceptor(instance);
  installCorrelationInterceptor(instance);
  installTenantInterceptor(instance);
  installErrorInterceptor(instance);

  return instance;
}

export const http: AxiosInstance = createHttp();
