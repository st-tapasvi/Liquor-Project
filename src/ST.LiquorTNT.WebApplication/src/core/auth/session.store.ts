import { create } from 'zustand';

import type { CurrentUserResponse, IstDateTime, SupplierCodeResponse } from '../api/contracts';

import { effectivePermissions, type PermissionKey } from './permissions';

export type SessionStatus = 'unknown' | 'anonymous' | 'authenticated';
export type SessionEndReason = 'logout' | 'timed_out' | 'invalid' | 'expired' | 'unauthenticated' | 'unreachable';

export interface SessionAccess {
  isSuperAdmin: boolean;
  granted: readonly string[];
  activeSupplierCode: SupplierCodeResponse | null;
  supplierCodes: readonly SupplierCodeResponse[];
}

export const NO_ACCESS: SessionAccess = {
  isSuperAdmin: false,
  granted: [],
  activeSupplierCode: null,
  supplierCodes: [],
};

export interface SessionUser {
  userId: number;
  userName: string;
  fullName: string | null;
  companyId: number | null;
  forcePasswordChange: boolean;
  passwordExpiresAt: IstDateTime | null;
  isSuperAdmin: boolean;
  activeSupplierCode: SupplierCodeResponse | null;
  supplierCodes: readonly SupplierCodeResponse[];
  permissions: ReadonlySet<PermissionKey>;
}

interface SessionState {
  status: SessionStatus;
  user: SessionUser | null;
  expiresAt: IstDateTime | null;
  idleTimeoutMinutes: number | null;
  endReason: SessionEndReason | null;

  setAuthenticated: (
    user: CurrentUserResponse,
    session?: { expiresAt: IstDateTime; idleTimeoutMinutes: number },
    access?: SessionAccess,
  ) => void;
  setAccess: (access: Partial<SessionAccess>) => void;
  setAnonymous: (reason: SessionEndReason | null) => void;
  renew: (session: { expiresAt: IstDateTime; idleTimeoutMinutes: number }) => void;
  clearEndReason: () => void;
}

export function toSessionUser(dto: CurrentUserResponse, access: SessionAccess = NO_ACCESS): SessionUser {
  return {
    userId: dto.userId,
    userName: dto.userName,
    fullName: dto.fullName,
    companyId: dto.companyId,
    forcePasswordChange: dto.forcePasswordChange,
    passwordExpiresAt: dto.passwordExpiresAt,
    isSuperAdmin: access.isSuperAdmin,
    activeSupplierCode: access.activeSupplierCode,
    supplierCodes: access.supplierCodes,
    permissions: effectivePermissions(access.granted),
  };
}

function accessOf(user: SessionUser): SessionAccess {
  return {
    isSuperAdmin: user.isSuperAdmin,
    granted: [...user.permissions],
    activeSupplierCode: user.activeSupplierCode,
    supplierCodes: user.supplierCodes,
  };
}

export const useSessionStore = create<SessionState>()((set) => ({
  status: 'unknown',
  user: null,
  expiresAt: null,
  idleTimeoutMinutes: null,
  endReason: null,

  setAuthenticated: (user, session, access) =>
    set((state) => ({
      status: 'authenticated',
      user: toSessionUser(user, access ?? (state.user ? accessOf(state.user) : NO_ACCESS)),
      expiresAt: session?.expiresAt ?? state.expiresAt,
      idleTimeoutMinutes: session?.idleTimeoutMinutes ?? state.idleTimeoutMinutes,
      endReason: null,
    })),

  setAccess: (access) =>
    set((state) => {
      if (!state.user) return state;
      const next = { ...accessOf(state.user), ...access };
      return {
        user: {
          ...state.user,
          isSuperAdmin: next.isSuperAdmin,
          activeSupplierCode: next.activeSupplierCode,
          supplierCodes: next.supplierCodes,
          permissions: effectivePermissions(next.granted),
        },
      };
    }),

  setAnonymous: (reason) =>
    set({ status: 'anonymous', user: null, expiresAt: null, idleTimeoutMinutes: null, endReason: reason }),

  renew: (session) => set({ expiresAt: session.expiresAt, idleTimeoutMinutes: session.idleTimeoutMinutes }),

  clearEndReason: () => set({ endReason: null }),
}));

export const sessionStore = {
  get: () => useSessionStore.getState(),
  setAnonymous: (reason: SessionEndReason | null) => useSessionStore.getState().setAnonymous(reason),
  clearActiveSupplierCode: () => useSessionStore.getState().setAccess({ activeSupplierCode: null, granted: [] }),
};
