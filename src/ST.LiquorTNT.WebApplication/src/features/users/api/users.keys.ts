import type { UserListParams } from '@/core/api';

export const userKeys = {
  all: (scope: number) => ['users', scope] as const,
  lists: (scope: number) => [...userKeys.all(scope), 'list'] as const,
  list: (scope: number, params: UserListParams) => [...userKeys.lists(scope), params] as const,
  detail: (scope: number, id: number) => [...userKeys.all(scope), 'detail', id] as const,
  access: (scope: number, id: number) => [...userKeys.all(scope), 'access', id] as const,
  roles: (scope: number) => ['roles', scope] as const,
};
