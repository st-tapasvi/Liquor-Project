import { http, HttpResponse } from 'msw';

import type { PasswordPolicyResponse, SecurityConfigResponse, UpdateSecurityConfigRequest } from '@/core/api';

import { problem } from '../problem';

export const mockSecurityConfig: SecurityConfigResponse[] = [
  {
    key: 'MAX_FAILED_LOGIN_ATTEMPTS',
    value: '3',
    dataType: 'INT',
    description: 'Wrong passwords in one day before the lock',
    updatedAt: null,
  },
  {
    key: 'SESSION_IDLE_MINUTES',
    value: '60',
    dataType: 'INT',
    description: 'Idle minutes before the session ends',
    updatedAt: null,
  },
  {
    key: 'SECURITY_QUESTION_ENABLED',
    value: '1',
    dataType: 'BOOL',
    description: 'Forgot password by security question',
    updatedAt: null,
  },
  { key: 'ADMIN_ROLE_ID', value: '1', dataType: 'INT', description: 'Which role counts as admin', updatedAt: null },
];

export const mockPolicies: PasswordPolicyResponse[] = [
  {
    id: 1,
    name: 'HARD',
    minLength: 12,
    maxLength: 64,
    requireUppercase: true,
    requireLowercase: true,
    requireNumber: true,
    requireSpecialCharacter: true,
    passwordHistoryCount: 5,
    passwordExpiryEnabled: true,
    passwordExpiryDays: 90,
    allowUsernameInPassword: false,
    allowCommonPassword: false,
  },
];

export const settingsHandlers = [
  http.get('/api/securityconfig', () => HttpResponse.json(mockSecurityConfig)),
  http.put('/api/securityconfig/:key', async ({ params, request }) => {
    const entry = mockSecurityConfig.find((e) => e.key === params['key']);
    if (!entry) return problem(404, 'NOT_FOUND', 'Unknown key.');
    const body = (await request.json()) as UpdateSecurityConfigRequest;
    if (entry.dataType === 'INT' && !/^\d+$/.test(body.value)) {
      return problem(400, 'VALIDATION_FAILED', 'Validation failed.', {
        errors: { value: ['Must be a whole number.'] },
      });
    }
    entry.value = body.value;
    entry.updatedAt = '2026-09-30T10:00:00' as never;
    return HttpResponse.json(entry);
  }),
  http.get('/api/passwordpolicies', () => HttpResponse.json(mockPolicies)),
  http.put('/api/passwordpolicies/:id', async ({ params, request }) => {
    const policy = mockPolicies.find((p) => p.id === Number(params['id']));
    if (!policy) return problem(404, 'NOT_FOUND', 'Unknown policy.');
    Object.assign(policy, await request.json());
    return HttpResponse.json(policy);
  }),
];
