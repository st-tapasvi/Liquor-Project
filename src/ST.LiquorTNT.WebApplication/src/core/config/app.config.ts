import { env } from './env';

export const appConfig = {
  name: env.VITE_APP_NAME,
  version: __APP_VERSION__,
  buildTime: __BUILD_TIME__,
  requestedWithHeader: 'X-Requested-With',
  requestedWithValue: 'XMLHttpRequest',
  csrfCookieName: 'XSRF-TOKEN',
  csrfHeaderName: 'X-XSRF-TOKEN',
  correlationHeader: 'X-Correlation-Id',
  requestTimeoutMs: 30_000,
  sessionWarningMinutes: 10,
} as const;
