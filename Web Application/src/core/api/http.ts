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
 * Authentication is a JWT carried in the HttpOnly `jwt` cookie (backed by a server-side USER_SESSION row), so
 * this code never sees it; `withCredentials` makes the browser attach it. There is no Authorization
 * header, no token refresh and nothing to store on the client.
 */
function createHttp(): AxiosInstance {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: appConfig.requestTimeoutMs,
    withCredentials: true,
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
