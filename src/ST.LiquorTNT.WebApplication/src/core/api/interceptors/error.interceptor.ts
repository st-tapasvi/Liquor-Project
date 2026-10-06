import { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig, isAxiosError, isCancel } from 'axios';

import { reauthStore } from '../../auth/reauth.store';
import { sessionStore } from '../../auth/session.store';
import { appConfig } from '../../config';
import { NetworkError, parseProblemDetails, SESSION_ENDED_CODES, SESSION_EXPIRED_CODE, toApiError } from '../../errors';
import { logger } from '../../logging/logger';
import { connectivity, requestConnectivityCheck } from '../../network';

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

export function installErrorInterceptor(http: AxiosInstance): void {
  http.interceptors.response.use(
    (response) => {
      connectivity.markOnline();
      return response;
    },
    async (error: unknown) => {
      if (isCancel(error)) throw error instanceof Error ? error : new Error('Request cancelled');
      if (!isAxiosError(error)) throw error instanceof Error ? error : new Error(String(error));

      const axiosError = error as AxiosError;
      const config = axiosError.config as (InternalAxiosRequestConfig & { meta?: RequestMeta }) | undefined;

      if (!axiosError.response) {
        const isTimeout = axiosError.code === 'ECONNABORTED' || axiosError.code === 'ETIMEDOUT';
        logger.warn('network error', { url: config?.url, code: axiosError.code });
        if (isTimeout) requestConnectivityCheck();
        else connectivity.markOffline();
        throw new NetworkError(axiosError.message, { cause: axiosError, isTimeout });
      }

      const { status, data, headers } = axiosError.response;

      if (isGatewayFailure(status, data)) {
        logger.warn('API unreachable behind a proxy', { url: config?.url, status });
        connectivity.markOffline();
        throw new NetworkError(`The API did not answer (HTTP ${status} from the proxy).`, { cause: axiosError });
      }
      connectivity.markOnline();

      const problem = parseProblemDetails(data);
      if (problem?.title === undefined && problem?.errorCode === undefined) {
        logger.warn('error response without a readable ProblemDetails body', {
          url: config?.url,
          status,
          contentType: String(headers['content-type'] ?? ''),
          bodyType: typeof data,
          bodyPreview: typeof data === 'string' ? data.slice(0, 300) : undefined,
        });
      }

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

function isGatewayFailure(status: number, body: unknown): boolean {
  if (status !== 502 && status !== 503 && status !== 504) return false;
  const problem = parseProblemDetails(body);
  return problem?.title === undefined && problem?.errorCode === undefined;
}
