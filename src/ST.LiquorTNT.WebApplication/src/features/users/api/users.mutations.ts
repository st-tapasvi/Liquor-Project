import { useMutation, useQueryClient } from '@tanstack/react-query';

import type { CreateUserRequest, UpdateUserRequest, UserResponse } from '@/core/api';
import { useCompanyScope } from '@/core/tenant';

import { useSnackbar } from '@/shared/hooks';

import { usersApi } from './users.api';
import { userKeys } from './users.keys';

/** Shared plumbing: after any change, refresh every cached view of users and announce success. */
function useUserMutation<TVariables>(
  key: string,
  mutationFn: (variables: TVariables) => Promise<UserResponse>,
  message: (user: UserResponse) => string,
  options?: { silent?: boolean },
) {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const companyId = useCompanyScope();
  return useMutation({
    mutationKey: ['users', key],
    mutationFn,
    // Validation errors are shown on the form; everything else through the global handler.
    meta: { silent: options?.silent ?? false },
    onSuccess: async (user) => {
      qc.setQueryData(userKeys.detail(companyId, user.id), user);
      await qc.invalidateQueries({ queryKey: userKeys.lists(companyId) });
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
