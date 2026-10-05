import { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig, isAxiosError, isCancel } from 'axios';

import { reauthStore } from '../../auth/reauth.store';
import { sessionStore } from '../../auth/session.store';
import { appConfig } from '../../config';
import { NetworkError, SESSION_ENDED_CODES, SESSION_EXPIRED_CODE, toApiError } from '../../errors';
import { logger } from '../../logging/logger';

/** Per-request flags. Set with `http.get(url, { meta: { … } })`. */
export interface RequestMeta {
  /** True for the login call made by the re-authentication dialog: its own 401 must not open the dialog again. */
  skipReauth?: boolean;
  /** True for calls where a 401 is a normal answer (e.g. the boot-time /me check) and must not clear state. */
  silentUnauthorized?: boolean;
  /** Internal: set when a request has already been retried after re-authentication. */
  retriedAfterReauth?: boolean;
}

declare module 'axios' {
  interface AxiosRequestConfig {
    meta?: RequestMeta;
  }
}

/**
 * Turns every failure into one of the AppError classes and implements the session rules:
 *
 *  - SESSION_EXPIRED (hard limit): park the request, open the password dialog, retry after re-login.
 *  - SESSION_TIMED_OUT / SESSION_INVALID / UNAUTHENTICATED: end the session; the router shows login.
 *  - Everything else: ApiError / ValidationError / NetworkError for the caller.
 */
export function installErrorInterceptor(http: AxiosInstance): void {
  http.interceptors.response.use(
    (response) => response,
    async (error: unknown) => {
      if (isCancel(error)) throw error instanceof Error ? error : new Error('Request cancelled');
      if (!isAxiosError(error)) throw error instanceof Error ? error : new Error(String(error));

      const axiosError = error as AxiosError;
      const config = axiosError.config as (InternalAxiosRequestConfig & { meta?: RequestMeta }) | undefined;

      if (!axiosError.response) {
        const isTimeout = axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT';
        logger.warn('network error', { url: config?.url, code: axiosError.code });
        throw new NetworkError(axiosError.message, { cause: axiosError, isTimeout });
      }

      const { status, data, headers } = axiosError.response;
      const echoed: unknown = headers[appConfig.correlationHeader.toLowerCase()];
      const apiError = toApiError(status, data, typeof echoed === 'string' ? echoed : undefined, axiosError);

      if (status === 401 && config) {
        if (apiError.code === SESSION_EXPIRED_CODE && !config.meta?.skipReauth && !config.meta?.retriedAfterReauth) {
          logger.info('session reached its hard limit; waiting for re-authentication', { url: config.url });
          await reauthStore.waitForReauth(); // rejects if the user cancels
          return http.request({ ...config, meta: { ...config.meta, retriedAfterReauth: true } });
        }

        if (
          SESSION_ENDED_CODES.includes(apiError.code as (typeof SESSION_ENDED_CODES)[number]) &&
          !config.meta?.silentUnauthorized
        ) {
          const reason =
            apiError.code === 'SESSION_TIMED_OUT'
              ? 'timed_out'
              : apiError.code === 'SESSION_INVALID'
                ? 'invalid'
                : 'unauthenticated';
          logger.info('session ended', { reason, url: config.url });
          sessionStore.setAnonymous(reason);
        }
      }

      if (status >= 500) {
        logger.error('api error', {
          url: config?.url,
          status,
          code: apiError.code,
          correlationId: apiError.correlationId,
        });
      }

      throw apiError;
    },
  );
}
