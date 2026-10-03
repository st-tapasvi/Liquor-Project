import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { isApiError } from '../errors';
import { logger } from '../logging/logger';

import { authApi } from './auth.api';
import { useSessionStore } from './session.store';
import { tokenStore } from './token.store';

interface AuthProviderProps {
  children: ReactNode;
  /** Shown while the start-up /me check is in flight (status 'unknown'). */
  fallback: ReactNode;
}

/**
 * Restores the session at start-up and clears cached server data whenever the session ends.
 *
 * On load the store is `unknown`. With no token in this tab the user is simply `anonymous` (no request).
 * With one, GET /api/auth/me decides: `authenticated` when the server still honours the session,
 * `anonymous` otherwise — the server-side session is the only source of truth, so a revoked or expired
 * session can never be "remembered" by the client.
 */
export function AuthProvider({ children, fallback }: AuthProviderProps) {
  const status = useSessionStore((s) => s.status);
  const setAuthenticated = useSessionStore((s) => s.setAuthenticated);
  const setAnonymous = useSessionStore((s) => s.setAnonymous);
  const queryClient = useQueryClient();

  // Why an effect: this is a one-time side effect (network call) that decides the initial state.
  useEffect(() => {
    if (status !== 'unknown') return;
    let cancelled = false;

    if (!tokenStore.has()) {
      setAnonymous(null);
      return;
    }

    authApi
      .me()
      .then((user) => {
        if (!cancelled) setAuthenticated(user);
      })
      .catch((error: unknown) => {
        if (cancelled) return;
        if (isApiError(error) && error.status === 401) {
          tokenStore.clear(); // the server no longer honours this token
          setAnonymous(null);
        } else {
          // The API is unreachable or broken: treat as anonymous so the login page can show the error.
          logger.warn('session check failed', { error });
          setAnonymous(null);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [status, setAuthenticated, setAnonymous]);

  // Why an effect: when the session ends for any reason, server data cached for the previous user
  // must not survive into the next login.
  useEffect(() => {
    if (status === 'anonymous') queryClient.clear();
  }, [status, queryClient]);

  if (status === 'unknown') return fallback;
  return children;
}
