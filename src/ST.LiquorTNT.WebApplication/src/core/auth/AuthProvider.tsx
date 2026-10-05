import { useQueryClient } from '@tanstack/react-query';
import { type ReactNode, useEffect } from 'react';

import { isApiError } from '../errors';
import { logger } from '../logging/logger';

import { authApi } from './auth.api';
import { useSessionStore } from './session.store';

interface AuthProviderProps {
  children: ReactNode;
  /** Shown while the start-up /me check is in flight (status 'unknown'). */
  fallback: ReactNode;
}

/**
 * Restores the session at start-up and clears cached server data whenever the session ends.
 *
 * On load the store is `unknown`; one GET /api/auth/me decides. The browser sends the HttpOnly `jwt`
 * cookie by itself (script cannot even tell whether it exists), so every tab asks the same question and
 * gets the same answer: 200 → `authenticated`, 401 → `anonymous` (login page). Nothing is read from
 * browser storage – the server-side session is the only source of truth, so a session revoked or logged
 * out in another tab can never be "remembered" here.
 *
 * A network failure or a 5xx is NOT a logout: the user may well still have a session. The app still has to
 * leave the loading screen, so it shows the login page with an "unreachable" notice rather than a session
 * message, and nothing redirects in a loop (the login page makes no further /me call).
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

  // Why an effect: when the session ends for any reason, server data cached for the previous user
  // must not survive into the next login.
  useEffect(() => {
    if (status === 'anonymous') queryClient.clear();
  }, [status, queryClient]);

  if (status === 'unknown') return fallback;
  return children;
}
