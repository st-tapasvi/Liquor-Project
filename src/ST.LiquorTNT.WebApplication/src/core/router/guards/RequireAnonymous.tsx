import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import { useSession } from '../../auth/useSession';
import { safeRedirectPath } from '../paths';

export function RequireAnonymous({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useSession();
  const location = useLocation();

  if (isAuthenticated) {
    const from = (location.state as { from?: unknown } | null)?.from;
    return <Navigate to={safeRedirectPath(typeof from === 'string' ? from : null)} replace />;
  }
  return children;
}
