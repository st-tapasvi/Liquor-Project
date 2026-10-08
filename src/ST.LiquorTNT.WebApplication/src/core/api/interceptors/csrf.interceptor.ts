import type { AxiosInstance } from 'axios';

import { appConfig } from '../../config';

export function installCsrfInterceptor(http: AxiosInstance): void {
  http.interceptors.request.use((config) => {
    config.headers.set(appConfig.requestedWithHeader, appConfig.requestedWithValue);
    return config;
  });
}
