import { useMutation, useQueryClient } from '@tanstack/react-query';

import type { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/core/api';
import { useCompanyScope } from '@/core/auth';

import { useSnackbar } from '@/shared/hooks';

import { usersApi } from './users.api';
import { userKeys } from './users.keys';

function useUserMutation<TVariables>(
  key: string,
  mutationFn: (variables: TVariables) => Promise<UserResponse>,
  message: (user: UserResponse) => string,
  options?: { silent?: boolean },
) {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['users', key],
    mutationFn,
    meta: { silent: options?.silent ?? false },
    onSuccess: async (user) => {
      qc.setQueryData(userKeys.detail(scope, user.id), user);
      await qc.invalidateQueries({ queryKey: userKeys.lists(scope) });
      snackbar.success(message(user));
    },
  });
}

export function useCreateUser() {
  return useUserMutation(
    'create',
    (request: CreateUserRequest) => usersApi.create(request),
    (u) => `User ${u.userName} created.`,
    {
      silent: true,
    },
  );
}

export function useUpdateUser(id: number) {
  return useUserMutation(
    'update',
    (request: UpdateUserRequest) => usersApi.update(id, request),
    (u) => `User ${u.userName} saved.`,
    {
      silent: true,
    },
  );
}

export function useDeleteUser() {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['users', 'delete'],
    mutationFn: (user: Pick<UserResponse, 'id' | 'userName'>) => usersApi.remove(user.id),
    onSuccess: async (_data, user) => {
      qc.removeQueries({ queryKey: userKeys.detail(scope, user.id) });
      qc.removeQueries({ queryKey: userKeys.access(scope, user.id) });
      await qc.invalidateQueries({ queryKey: userKeys.lists(scope) });
      snackbar.success(`User ${user.userName} deleted.`);
    },
  });
}

export function useActivateUser() {
  return useUserMutation(
    'activate',
    (id: number) => usersApi.activate(id),
    (u) => `User ${u.userName} activated.`,
  );
}

export function useDeactivateUser() {
  return useUserMutation(
    'deactivate',
    (id: number) => usersApi.deactivate(id),
    (u) => `User ${u.userName} deactivated.`,
  );
}

export function useUnlockUser() {
  return useUserMutation(
    'unlock',
    (id: number) => usersApi.unlock(id),
    (u) => `User ${u.userName} unlocked.`,
  );
}
