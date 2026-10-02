import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import type { UpdatePasswordPolicyRequest, UpdateSecurityConfigRequest } from '@/core/api';
import { useCompanyScope } from '@/core/tenant';

import { useSnackbar } from '@/shared/hooks';

import { settingsApi } from './settings.api';
import { settingsKeys } from './settings.keys';

export function useSecurityConfig() {
  const companyId = useCompanyScope();
  return useQuery({ queryKey: settingsKeys.securityConfig(companyId), queryFn: settingsApi.securityConfig });
}

export function useUpdateSecurityConfig() {
  const companyId = useCompanyScope();
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  return useMutation({
    mutationKey: ['settings', 'security-config', 'update'],
    meta: { silent: true },
    mutationFn: ({ key, request }: { key: string; request: UpdateSecurityConfigRequest }) =>
      settingsApi.updateSecurityConfig(key, request),
    onSuccess: async (entry) => {
      await qc.invalidateQueries({ queryKey: settingsKeys.securityConfig(companyId) });
      snackbar.success(`${entry.key} saved. It applies from the next request.`);
    },
  });
}

export function usePasswordPolicies() {
  const companyId = useCompanyScope();
  return useQuery({ queryKey: settingsKeys.passwordPolicies(companyId), queryFn: settingsApi.passwordPolicies });
}

export function useUpdatePasswordPolicy() {
  const companyId = useCompanyScope();
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  return useMutation({
    mutationKey: ['settings', 'password-policies', 'update'],
    meta: { silent: true },
    mutationFn: ({ id, request }: { id: number; request: UpdatePasswordPolicyRequest }) =>
      settingsApi.updatePasswordPolicy(id, request),
    onSuccess: async (policy) => {
      await qc.invalidateQueries({ queryKey: settingsKeys.passwordPolicies(companyId) });
      snackbar.success(`Policy ${policy.name} saved.`);
    },
  });
}
