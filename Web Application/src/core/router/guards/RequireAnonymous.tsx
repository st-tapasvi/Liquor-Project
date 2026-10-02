import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import { useSession } from '../../auth/useSession';
import { safeRedirectPath } from '../paths';

/**
 * Login and recovery pages: a signed-in user is sent on – back to the screen that required login
 * (carried in router state by RequireAuth, validated against open redirects) or to the dashboard.
 * Because this guard reacts to the session store, the login form itself never has to navigate.
 */
export function RequireAnonymous({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useSession();
  const location = useLocation();

  if (isAuthenticated) {
    const from = (location.state as { from?: unknown } | null)?.from;
    return <Navigate to={safeRedirectPath(typeof from === 'string' ? from : null)} replace />;
  }
  return children;
}
