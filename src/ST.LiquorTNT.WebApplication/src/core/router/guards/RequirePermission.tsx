import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';

import type { PermissionKey } from '../../auth/permissions';
import { useAnyPermission } from '../../auth/useSession';
import { PATHS } from '../paths';

interface ForbiddenState {
  from: string;
  rights: readonly PermissionKey[];
}

export function RequirePermission({
  right,
  children,
}: {
  right: PermissionKey | readonly PermissionKey[];
  children: ReactNode;
}) {
  const location = useLocation();
  const keys = Array.isArray(right) ? (right as readonly PermissionKey[]) : [right as PermissionKey];
  const allowed = useAnyPermission(keys);
  if (!allowed) {
    const state: ForbiddenState = { from: location.pathname, rights: keys };
    return <Navigate to={PATHS.forbidden} replace state={state} />;
  }
  return children;
}
