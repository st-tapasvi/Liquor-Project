import { keepPreviousData, useQuery } from '@tanstack/react-query';

import type { UserListParams } from '@/core/api';
import { useCompanyScope } from '@/core/tenant';

import { usersApi } from './users.api';
import { userKeys } from './users.keys';

export function useUsers(params: UserListParams) {
  const companyId = useCompanyScope();

  return useQuery({
    queryKey: userKeys.list(companyId, params),
    queryFn: () => usersApi.list(params),
    placeholderData: keepPreviousData,
  });
}

export function useUser(id: number | undefined) {
  const companyId = useCompanyScope();

  return useQuery({
    queryKey: userKeys.detail(companyId, id ?? -1),
    queryFn: () => usersApi.getById(id ?? -1),
    enabled: id !== undefined,
  });
}
