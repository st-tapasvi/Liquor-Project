import { create } from 'zustand';

import type { CurrentUserResponse, IstDateTime } from '../api/contracts';

import { type PermissionKey, interimRights, toPermissionSet } from './permissions';
import { tokenStore } from './token.store';

/**
 * Session state as the browser knows it. It holds no credential — the bearer token lives in
 * `token.store`, read only by the HTTP client. This store is a cache of "who is logged in" for rendering.
 *
 * - `unknown`       app just started; AuthProvider is asking GET /api/auth/me
 * - `anonymous`     no valid session (never logged in, logged out, timed out, revoked)
 * - `authenticated` the API confirmed the session
 *
 * The store is in memory only and is never persisted. A page reload goes through `unknown` → /me
 * again (with the restored token), which is exactly the check the server-side session needs.
 */
export type SessionStatus = 'unknown' | 'anonymous' | 'authenticated';

/** Why the last session ended: shown on the login page so the user understands what happened. */
export type SessionEndReason = 'logout' | 'timed_out' | 'invalid' | 'expired' | 'unauthenticated';

export interface SessionUser {
  userId: number;
  userName: string;
  fullName: string | null;
  roleId: number | null;
  companyId: number | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
  isAdministrator: boolean;
  permissions: ReadonlySet<PermissionKey>;
}

interface SessionState {
  status: SessionStatus;
  user: SessionUser | null;
  /** Hard limit of the current session (IST string from the API). */
  expiresAt: IstDateTime | null;
  idleTimeoutMinutes: number | null;
  endReason: SessionEndReason | null;

  setAuthenticated: (
    user: CurrentUserResponse,
    session?: { expiresAt: IstDateTime; idleTimeoutMinutes: number },
  ) => void;
  setAnonymous: (reason: SessionEndReason | null) => void;
  /** Called after a re-login in place (SESSION_EXPIRED): only the deadline changes. */
  renew: (session: { expiresAt: IstDateTime; idleTimeoutMinutes: number }) => void;
  clearEndReason: () => void;
}

export function toSessionUser(dto: CurrentUserResponse): SessionUser {
  const fallback = interimRights(dto.roleId);
  const isAdministrator = dto.isAdministrator ?? fallback.isAdministrator;
  const permissions = dto.permissions ?? fallback.permissions;

  return {
    userId: dto.userId,
    userName: dto.userName,
    fullName: dto.fullName,
    roleId: dto.roleId,
    companyId: dto.companyId,
    forcePasswordChange: dto.forcePasswordChange,
    passwordExpiresAt: dto.passwordExpiresAt,
    isAdministrator,
    permissions: toPermissionSet(permissions),
  };
}

export const useSessionStore = create<SessionState>()((set) => ({
  status: 'unknown',
  user: null,
  expiresAt: null,
  idleTimeoutMinutes: null,
  endReason: null,

  setAuthenticated: (user, session) =>
    set((state) => ({
      status: 'authenticated',
      user: toSessionUser(user),
      expiresAt: session?.expiresAt ?? state.expiresAt,
      idleTimeoutMinutes: session?.idleTimeoutMinutes ?? state.idleTimeoutMinutes,
      endReason: null,
    })),

  setAnonymous: (reason) => {
    tokenStore.clear(); // whatever ended the session, the token must not outlive it
    set({ status: 'anonymous', user: null, expiresAt: null, idleTimeoutMinutes: null, endReason: reason });
  },

  renew: (session) => set({ expiresAt: session.expiresAt, idleTimeoutMinutes: session.idleTimeoutMinutes }),

  clearEndReason: () => set({ endReason: null }),
}));

/** Non-hook access for interceptors and the logger (outside React). */
export const sessionStore = {
  get: () => useSessionStore.getState(),
  setAnonymous: (reason: SessionEndReason | null) => useSessionStore.getState().setAnonymous(reason),
};
