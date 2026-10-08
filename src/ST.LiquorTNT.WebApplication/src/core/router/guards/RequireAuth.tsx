import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import { useSession } from '../../auth/useSession';
import { PATHS } from '../paths';

export function RequireAuth({ children }: { children: ReactNode }) {
  const { isAuthenticated } = useSession();
  const location = useLocation();

  if (!isAuthenticated) {
    return <Navigate to={PATHS.login} replace state={{ from: `${location.pathname}${location.search}` }} />;
  }
  return children;
}
