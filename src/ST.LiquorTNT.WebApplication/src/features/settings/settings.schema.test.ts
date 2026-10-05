import { passwordPolicySchema, validateConfigValue } from './settings.schema';

describe('settings schema', () => {
  it('validates config values by data type', () => {
    expect(validateConfigValue('INT', '5')).toBeNull();
    expect(validateConfigValue('INT', '0')).not.toBeNull();
    expect(validateConfigValue('INT', 'x')).not.toBeNull();
    expect(validateConfigValue('BOOL', 'true')).toBeNull();
    expect(validateConfigValue('BOOL', 'yes')).not.toBeNull();
    expect(validateConfigValue('STRING', '')).not.toBeNull();
  });

  it('rejects max below min and expiry without days', () => {
    const base = {
      minLength: 8,
      maxLength: 6,
      requireUppercase: true,
      requireLowercase: true,
      requireNumber: true,
      requireSpecialCharacter: false,
      passwordHistoryCount: 5,
      passwordExpiryEnabled: true,
      passwordExpiryDays: null,
      allowUsernameInPassword: false,
      allowCommonPassword: false,
    };
    const r = passwordPolicySchema.safeParse(base);
    expect(r.success).toBe(false);
    const paths = r.success ? [] : r.error.issues.map((i) => i.path.join('.'));
    expect(paths).toContain('maxLength');
    expect(paths).toContain('passwordExpiryDays');
  });
});
