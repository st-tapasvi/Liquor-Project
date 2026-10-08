import type { ReactNode } from 'react';

import type { PermissionKey } from './permissions';
import { useAnyPermission } from './useSession';

interface CanProps {
  right: PermissionKey | readonly PermissionKey[];
  children: ReactNode;
  fallback?: ReactNode;
}


export function Can({ right, children, fallback = null }: CanProps) {
  const keys = Array.isArray(right) ? (right as readonly PermissionKey[]) : [right as PermissionKey];
  const allowed = useAnyPermission(keys);
  return allowed ? children : fallback;
}
