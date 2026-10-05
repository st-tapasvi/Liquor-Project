import type { ReactNode } from 'react';

import type { PermissionKey } from './permissions';
import { useAnyPermission } from './useSession';

interface CanProps {
  /** One right, or several of which any one is enough. */
  right: PermissionKey | readonly PermissionKey[];
  children: ReactNode;
  fallback?: ReactNode;
}

/**
 * Renders children only when the user holds the right. This is a usability aid: the API enforces
 * authorization on every call regardless of what the UI shows.
 */
export function Can({ right, children, fallback = null }: CanProps) {
  const keys = Array.isArray(right) ? (right as readonly PermissionKey[]) : [right as PermissionKey];
  const allowed = useAnyPermission(keys);
  return allowed ? children : fallback;
}
