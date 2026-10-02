import type { CurrentUserResponse, IstDateTime, UserResponse } from '@/core/api';
import { PERMISSION_KEYS } from '@/core/auth';

let nextId = 100;

export function makeUser(overrides: Partial<UserResponse> = {}): UserResponse {
  const id = overrides.id ?? nextId++;
  return {
    id,
    userName: `user${id}`,
    fullName: `User ${id}`,
    email: null,
    phone: null,
    employeeCode: null,
    roleId: 2,
    companyId: null,
    isActive: true,
    isBlocked: false,
    failedLoginAttempts: 0,
    lockedUntil: null,
    forcePasswordChange: false,
    passwordExpiresAt: null,
    lastLoginAt: '2026-09-28T09:00:00' as IstDateTime,
    createdAt: '2026-09-27T16:20:00' as IstDateTime,
    ...overrides,
  };
}

export function makeCurrentUser(overrides: Partial<CurrentUserResponse> = {}): CurrentUserResponse {
  return {
    userId: 1,
    userName: 'admin',
    fullName: 'Administrator',
    roleId: 1,
    companyId: null,
    forcePasswordChange: false,
    passwordExpiresAt: null,
    isAdministrator: true,
    // The test admin holds every right, mirroring the API's single admin role today.
    permissions: [...PERMISSION_KEYS],
    ...overrides,
  };
}
