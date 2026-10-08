import type { ReactNode } from 'react';
import { Navigate } from 'react-router';

import type { PermissionKey } from '../../auth/permissions';
import { useAnyPermission } from '../../auth/useSession';
import { PATHS } from '../paths';

export function RequirePermission({
  right,
  children,
}: {
  right: PermissionKey | readonly PermissionKey[];
  children: ReactNode;
}) {
  const keys = Array.isArray(right) ? (right as readonly PermissionKey[]) : [right as PermissionKey];
  const allowed = useAnyPermission(keys);
  if (!allowed) return <Navigate to={PATHS.forbidden} replace />;
  return children;
}
