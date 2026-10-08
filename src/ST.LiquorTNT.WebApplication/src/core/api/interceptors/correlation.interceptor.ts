import type { AxiosInstance } from 'axios';

import { appConfig } from '../../config';
import { setLastCorrelationId } from '../../logging/logger';

export function installCorrelationInterceptor(http: AxiosInstance): void {
  http.interceptors.request.use((config) => {
    const id = crypto.randomUUID().replaceAll('-', '');
    config.headers.set(appConfig.correlationHeader, id);
    setLastCorrelationId(id);
    return config;
  });

  http.interceptors.response.use(
    (response) => {
      const echoed: unknown = response.headers[appConfig.correlationHeader.toLowerCase()];
      if (typeof echoed === 'string') setLastCorrelationId(echoed);
      return response;
    },
    (error: unknown) => Promise.reject(error instanceof Error ? error : new Error(String(error))),
  );
}
