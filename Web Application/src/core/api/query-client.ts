import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';

import { isClientError, isNetworkError } from '../errors';
import { logger } from '../logging/logger';

/** Set by the app shell so global failures can reach the snackbar without importing UI into core. */
type MutationErrorListener = (error: unknown) => void;
let mutationErrorListener: MutationErrorListener | null = null;
export function setGlobalMutationErrorListener(listener: MutationErrorListener | null): void {
  mutationErrorListener = listener;
}

/**
 * Project-wide TanStack Query defaults.
 *  - never retry a 4xx (the caller's fault), retry network/5xx twice
 *  - no refetch on window focus: an on-premise LAN app with heavy grids should not reload by surprise
 *  - every failed query/mutation is logged once, here, with its key
 */
export function createQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30_000,
        gcTime: 5 * 60_000,
        retry: (failureCount, error) => !isClientError(error) && failureCount < 2,
        retryDelay: (attempt) => Math.min(1000 * 2 ** attempt, 8000),
        refetchOnWindowFocus: false,
        refetchOnReconnect: true,
        throwOnError: (error) => !isClientError(error) && !isNetworkError(error),
      },
      mutations: { retry: 0 },
    },
    queryCache: new QueryCache({
      onError: (error, query) => logger.warn('query failed', { key: query.queryKey, error }),
    }),
    mutationCache: new MutationCache({
      onError: (error, _variables, _context, mutation) => {
        logger.warn('mutation failed', { key: mutation.options.mutationKey, error });
        // Validation errors are handled by the form; everything else is announced globally.
        if (!(mutation.meta?.['silent'] === true)) mutationErrorListener?.(error);
      },
    }),
  });
}

export const queryClient: QueryClient = createQueryClient();
