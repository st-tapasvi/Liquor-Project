import { z } from 'zod';

import type { PasswordPolicyResponse, UpdatePasswordPolicyRequest } from '@/core/api';

/** Mirrors the API rules for PUT /api/passwordpolicies/{id}. The API remains authoritative. */
export const passwordPolicySchema = z
  .object({
    minLength: z.number().int().min(1).max(128),
    maxLength: z.number().int().min(1).max(128),
    requireUppercase: z.boolean(),
    requireLowercase: z.boolean(),
    requireNumber: z.boolean(),
    requireSpecialCharacter: z.boolean(),
    passwordHistoryCount: z.number().int().min(0).max(50),
    passwordExpiryEnabled: z.boolean(),
    passwordExpiryDays: z.number().int().min(1).nullable(),
    allowUsernameInPassword: z.boolean(),
    allowCommonPassword: z.boolean(),
  })
  .refine((v) => v.maxLength >= v.minLength, { path: ['maxLength'], message: 'Maximum must not be below minimum.' })
  .refine((v) => !v.passwordExpiryEnabled || (v.passwordExpiryDays ?? 0) >= 1, {
    path: ['passwordExpiryDays'],
    message: 'Enter the number of days when expiry is on.',
  });

export type PasswordPolicyFormValues = z.infer<typeof passwordPolicySchema>;

export function fromPolicy(p: PasswordPolicyResponse): PasswordPolicyFormValues {
  return {
    minLength: p.minLength,
    maxLength: p.maxLength,
    requireUppercase: p.requireUppercase,
    requireLowercase: p.requireLowercase,
    requireNumber: p.requireNumber,
    requireSpecialCharacter: p.requireSpecialCharacter,
    passwordHistoryCount: p.passwordHistoryCount,
    passwordExpiryEnabled: p.passwordExpiryEnabled,
    passwordExpiryDays: p.passwordExpiryDays,
    allowUsernameInPassword: p.allowUsernameInPassword,
    allowCommonPassword: p.allowCommonPassword,
  };
}

export function toPolicyRequest(v: PasswordPolicyFormValues): UpdatePasswordPolicyRequest {
  return { ...v, passwordExpiryDays: v.passwordExpiryEnabled ? v.passwordExpiryDays : null };
}

/** Security config values are strings typed by `dataType`; validate what we can before sending. */
export function validateConfigValue(dataType: string, value: string): string | null {
  const v = value.trim();
  if (v.length === 0) return 'A value is required.';
  if (dataType === 'INT') return /^\d+$/.test(v) && Number(v) >= 1 ? null : 'Enter a whole number, 1 or more.';
  if (dataType === 'BOOL') return /^(1|0|true|false)$/i.test(v) ? null : 'Enter 1/0 or true/false.';
  return v.length > 200 ? 'At most 200 characters.' : null;
}
