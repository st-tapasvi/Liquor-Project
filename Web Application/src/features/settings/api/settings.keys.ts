/**
 * Query-key factory for the settings module (specification §7).
 *
 * Security configuration and password policies are per-company, so the keys are scoped by company for
 * the same reason the user keys are (§9).
 */
export const settingsKeys = {
  all: (companyId: number) => ['settings', companyId] as const,
  securityConfig: (companyId: number) => [...settingsKeys.all(companyId), 'security-config'] as const,
  passwordPolicies: (companyId: number) => [...settingsKeys.all(companyId), 'password-policies'] as const,
};
