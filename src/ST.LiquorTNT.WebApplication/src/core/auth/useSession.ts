import { useShallow } from 'zustand/react/shallow';

import { hasAnyPermission, hasPermission, type PermissionKey } from './permissions';
import { type SessionUser, useSessionStore } from './session.store';

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

export function useCurrentUser(): SessionUser {
  const user = useSessionStore((s) => s.user);
  if (!user) throw new Error('useCurrentUser() called outside an authenticated route');
  return user;
}

export function usePermission(key: PermissionKey): boolean {
  return useSessionStore((s) => (s.user ? s.user.isSuperAdmin || hasPermission(s.user.permissions, key) : false));
}

export function useAnyPermission(keys: readonly PermissionKey[]): boolean {
  return useSessionStore((s) => (s.user ? s.user.isSuperAdmin || hasAnyPermission(s.user.permissions, keys) : false));
}

export function useCompanyScope(): number {
  return useSessionStore((s) => s.user?.activeSupplierCode?.id ?? 0);
}
