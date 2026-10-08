import axios, { type AxiosInstance } from 'axios';

import { appConfig, env } from '../config';

import { installCorrelationInterceptor, installCsrfInterceptor, installErrorInterceptor } from './interceptors';

function createHttp(): AxiosInstance {
  const instance = axios.create({
    baseURL: env.VITE_API_BASE_URL,
    timeout: appConfig.requestTimeoutMs,
    withCredentials: true,
    xsrfCookieName: appConfig.csrfCookieName,
    xsrfHeaderName: appConfig.csrfHeaderName,
    withXSRFToken: true,
    headers: { Accept: 'application/json' },
    paramsSerializer: { indexes: null },
    validateStatus: (status) => status >= 200 && status < 300,
  });

  installCsrfInterceptor(instance);
  installCorrelationInterceptor(instance);
  installErrorInterceptor(instance);

  return instance;
}

export const http: AxiosInstance = createHttp();
