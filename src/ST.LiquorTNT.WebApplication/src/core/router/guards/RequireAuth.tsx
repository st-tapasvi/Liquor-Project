import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import { useSession } from '../../auth/useSession';
import { PATHS } from '../paths';

/**
 * Only authenticated users pass. Anyone else is sent to the login page; the attempted location is
 * carried in router state (never in the URL) so login can return the user there afterwards.
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useSession();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to={PATHS.login} replace state={{ from: `${location.pathname}${location.search}` }} />;
  }
  return children;
}
