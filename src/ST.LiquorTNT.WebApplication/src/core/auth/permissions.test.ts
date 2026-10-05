import type { CurrentUserResponse } from '../api/contracts';

import { EVERYONE_PERMISSIONS, PERMISSION_KEYS, interimRights } from './permissions';
import { toSessionUser } from './session.store';

const base: CurrentUserResponse = {
  userId: 7,
  userName: 'alice',
  fullName: 'Alice',
  roleId: 2,
  companyId: null,
  forcePasswordChange: false,
  passwordExpiresAt: null,
};

describe('interim rights (API sends no permissions yet)', () => {
  it('role 1 is the administrator and sees everything', () => {
    expect(interimRights(1)).toEqual({ isAdministrator: true, permissions: PERMISSION_KEYS });
    const user = toSessionUser({ ...base, roleId: 1 });
    expect(user.isAdministrator).toBe(true);
    expect(user.permissions.has('users.manage')).toBe(true);
  });

  it('any other role sees only what everyone may open', () => {
    expect(interimRights(2).permissions).toBe(EVERYONE_PERMISSIONS);
    const user = toSessionUser(base);
    expect(user.isAdministrator).toBe(false);
    expect([...user.permissions]).toEqual(['dashboard.view', 'sessions.manage']);
  });

  it('prefers the values the API sends when they are present', () => {
    const user = toSessionUser({ ...base, roleId: 1, isAdministrator: false, permissions: ['reports.view'] });
    expect(user.isAdministrator).toBe(false);
    expect([...user.permissions]).toEqual(['reports.view']);
  });
});
