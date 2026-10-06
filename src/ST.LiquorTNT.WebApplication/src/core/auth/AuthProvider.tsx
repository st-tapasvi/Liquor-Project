import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { isApiError } from '../errors';
import { logger } from '../logging/logger';
import { useConnectivity } from '../network';

import { authApi } from './auth.api';
import { useSessionStore } from './session.store';

interface AuthProviderProps {
  children: ReactNode;
  /** Shown while the start-up /me check is in flight (status 'unknown'). */
  fallback: ReactNode;
}

export function AuthProvider({ children, fallback }: AuthProviderProps) {
  const status = useSessionStore((s) => s.status);
  const setAuthenticated = useSessionStore((s) => s.setAuthenticated);
  const setAnonymous = useSessionStore((s) => s.setAnonymous);
  const clearEndReason = useSessionStore((s) => s.clearEndReason);
  const endReason = useSessionStore((s) => s.endReason);
  const online = useConnectivity() === 'online';
  const queryClient = useQueryClient();

  // Why an effect: this is a one-time side effect (network call) that decides the initial state.
  useEffect(() => {
    if (status !== 'unknown') return;
    let cancelled = false;

    authApi
      .me()
      .then((user) => {
        if (!cancelled) setAuthenticated(user);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        if (isApiError(error) && error.status === 401) {
          setAnonymous(null);
        } else {
          logger.warn('session check failed; the API is unreachable or broken', { error });
          setAnonymous('unreachable');
        }
      });

    return () => {
      cancelled = true;
    };
  }, [status, setAuthenticated, setAnonymous]);

  useEffect(() => {
    if (!online || status !== 'anonymous' || endReason !== 'unreachable') return;
    let cancelled = false;

    authApi
      .me()
      .then((user) => {
        if (!cancelled) setAuthenticated(user);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        if (isApiError(error) && error.status === 401) clearEndReason();
      });

    return () => {
      cancelled = true;
    };
  }, [online, status, endReason, setAuthenticated, clearEndReason]);

  // Why an effect: when the session ends for any reason, server data cached for the previous user
  // must not survive into the next login.
  useEffect(() => {
    if (status === 'anonymous') queryClient.clear();
  }, [status, queryClient]);

  if (status === 'unknown') return fallback;
  return children;
}
