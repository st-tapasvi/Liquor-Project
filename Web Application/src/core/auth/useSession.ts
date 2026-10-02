import { useShallow } from 'zustand/react/shallow';

import { hasAnyPermission, hasPermission, type PermissionKey } from './permissions';
import { type SessionUser, useSessionStore } from './session.store';

/** The current user and session facts, for components. Subscribes only to what it returns. */
export function useSession() {
  return useSessionStore(
    useShallow((s) => ({
      status: s.status,
      user: s.user,
      expiresAt: s.expiresAt,
      idleTimeoutMinutes: s.idleTimeoutMinutes,
      endReason: s.endReason,
      isAuthenticated: s.status === 'authenticated',
    })),
  );
}

/** The authenticated user. Only call inside routes protected by RequireAuth. */
export function useCurrentUser(): SessionUser {
  const user = useSessionStore((s) => s.user);
  if (!user) throw new Error('useCurrentUser() called outside an authenticated route');
  return user;
}

export function usePermission(key: PermissionKey): boolean {
  return useSessionStore((s) => (s.user ? hasPermission(s.user.permissions, key) : false));
}

export function useAnyPermission(keys: readonly PermissionKey[]): boolean {
  return useSessionStore((s) => (s.user ? hasAnyPermission(s.user.permissions, keys) : false));
}
