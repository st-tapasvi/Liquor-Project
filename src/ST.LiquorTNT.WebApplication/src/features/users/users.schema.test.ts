import { createUserSchema, fromUser, toCreateRequest, toUpdateRequest } from './users.schema';

describe('users schema', () => {
  it('accepts a valid create form and maps blanks to null', () => {
    const parsed = createUserSchema.safeParse({
      userName: 'ravi.k',
      password: 'Temp@Pass2026x',
      roleId: 2,
      companyId: null,
      fullName: '  ',
      email: '',
      phone: '',
      employeeCode: 'EMP-1',
      forcePasswordChange: true,
    });
    expect(parsed.success).toBe(true);
    if (!parsed.success) return;
    const req = toCreateRequest(parsed.data);
    expect(req.fullName).toBeNull();
    expect(req.email).toBeNull();
    expect(req.employeeCode).toBe('EMP-1');
  });

  it('rejects an invalid user name', () => {
    const parsed = createUserSchema.safeParse({
      userName: 'ravi k!',
      password: 'Temp@Pass2026x',
      roleId: 2,
      companyId: null,
      fullName: '',
      email: '',
      phone: '',
      employeeCode: '',
      forcePasswordChange: true,
    });
    expect(parsed.success).toBe(false);
  });

  it('rejects a malformed e-mail but accepts empty', () => {
    const base = {
      userName: 'a',
      password: 'Temp@Pass2026x',
      roleId: 1,
      companyId: null,
      fullName: '',
      phone: '',
      employeeCode: '',
      forcePasswordChange: false,
    };
    expect(createUserSchema.safeParse({ ...base, email: 'nope' }).success).toBe(false);
    expect(createUserSchema.safeParse({ ...base, email: '' }).success).toBe(true);
  });

  it('round-trips a user through the edit form', () => {
    const values = fromUser({
      id: 7,
      userName: 'ravi.k',
      fullName: 'Ravi Kumar',
      email: null,
      phone: '9876543210',
      employeeCode: null,
      roleId: 2,
      companyId: 5,
      isActive: true,
      isBlocked: false,
      failedLoginAttempts: 0,
      lockedUntil: null,
      forcePasswordChange: false,
      passwordExpiresAt: null,
      lastLoginAt: null,
      createdAt: '2026-09-27T16:20:00' as never,
    });
    expect(values.email).toBe('');
    expect(toUpdateRequest(values)).toEqual({
      roleId: 2,
      companyId: 5,
      fullName: 'Ravi Kumar',
      email: null,
      phone: '9876543210',
      employeeCode: null,
    });
  });
});
