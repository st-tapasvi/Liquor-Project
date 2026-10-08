import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { isApiError } from '../errors';
import { logger } from '../logging/logger';
import { useConnectivity } from '../network';

import { authApi, loadAccess } from './auth.api';
import { NO_ACCESS, useSessionStore } from './session.store';

interface AuthProviderProps {
  children: ReactNode;
  fallback: ReactNode;
}

async function restoreSession() {
  const user = await authApi.me();
  const access = await loadAccess().catch((error: unknown) => {
    if (isApiError(error) && error.status === 401) throw error;
    logger.warn('rights could not be loaded; continuing with none until the next reload', { error });
    return NO_ACCESS;
  });
  return { user, access };
}

export function AuthProvider({ children, fallback }: AuthProviderProps) {
  const status = useSessionStore((s) => s.status);
  const setAuthenticated = useSessionStore((s) => s.setAuthenticated);
  const setAnonymous = useSessionStore((s) => s.setAnonymous);
  const clearEndReason = useSessionStore((s) => s.clearEndReason);
  const endReason = useSessionStore((s) => s.endReason);
  const online = useConnectivity() === 'online';
  const queryClient = useQueryClient();

  useEffect(() => {
    if (status !== 'unknown') return;
    let cancelled = false;

    restoreSession()
      .then(({ user, access }) => {
        if (!cancelled) setAuthenticated(user, undefined, access);
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

    restoreSession()
      .then(({ user, access }) => {
        if (!cancelled) setAuthenticated(user, undefined, access);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        if (isApiError(error) && error.status === 401) clearEndReason();
      });

    return () => {
      cancelled = true;
    };
  }, [online, status, endReason, setAuthenticated, clearEndReason]);

  useEffect(() => {
    if (status === 'anonymous') queryClient.clear();
  }, [status, queryClient]);

  if (status === 'unknown') return fallback;
  return children;
}
