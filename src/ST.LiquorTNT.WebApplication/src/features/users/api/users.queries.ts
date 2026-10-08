import { keepPreviousData, useQuery } from '@tanstack/react-query';

import type { UserListParams } from '@/core/api';
import { useCompanyScope } from '@/core/auth';

import { rolesApi, userLookupsApi, usersApi } from './users.api';
import { userKeys } from './users.keys';

export function useUsers(params: UserListParams) {
  const scope = useCompanyScope();

  return useQuery({
    queryKey: userKeys.list(scope, params),
    queryFn: () => usersApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useUser(id: number | undefined) {
  const scope = useCompanyScope();

  return useQuery({
    queryKey: userKeys.detail(scope, id ?? -1),
    queryFn: () => usersApi.getById(id ?? -1),
    enabled: id !== undefined,
  });
}

export function useUserAccess(id: number) {
  const scope = useCompanyScope();
  return useQuery({ queryKey: userKeys.access(scope, id), queryFn: () => usersApi.access(id) });
}

export function useRoles(enabled = true) {
  const scope = useCompanyScope();
  return useQuery({ queryKey: userKeys.roles(scope), queryFn: rolesApi.list, enabled });
}

export function useCompanyOptions(enabled: boolean) {
  const scope = useCompanyScope();
  return useQuery({ queryKey: ['users', scope, 'lookup', 'companies'], queryFn: userLookupsApi.companies, enabled });
}

export function useSupplierCodeOptions(enabled: boolean) {
  const scope = useCompanyScope();
  return useQuery({
    queryKey: ['users', scope, 'lookup', 'supplier-codes'],
    queryFn: userLookupsApi.supplierCodes,
    enabled,
  });
}
