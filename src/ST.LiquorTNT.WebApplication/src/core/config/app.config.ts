import { env } from './env';

/** Static application facts. Version and build time are injected by Vite from package.json. */
export const appConfig = {
  name: env.VITE_APP_NAME,
  version: __APP_VERSION__,
  buildTime: __BUILD_TIME__,
  /** Header that every API request carries; the API refuses state-changing requests without it (CSRF). */
  requestedWithHeader: 'X-Requested-With',
  requestedWithValue: 'XMLHttpRequest',
  /** The readable CSRF cookie the API sets at login, echoed by axios in this header on every request. */
  csrfCookieName: 'XSRF-TOKEN',
  csrfHeaderName: 'X-XSRF-TOKEN',
  correlationHeader: 'X-Correlation-Id',
  /** Request timeout. Reports and imports that legitimately take longer override it per call. */
  requestTimeoutMs: 30_000,
  /** Warn the user this many minutes before the hard session limit. */
  sessionWarningMinutes: 10,
  platformApiAvailable: false as boolean,
} as const;
