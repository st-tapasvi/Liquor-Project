import { useMutation, useQueryClient } from '@tanstack/react-query';

import type {
  CreateUserRequest,
  RoleResponse,
  SaveRoleRequest,
  SupplierCodeResponse,
  UpdateUserRequest,
  UpdateUserRolesRequest,
  UserResponse,
} from '@/core/api';
import { useCompanyScope } from '@/core/auth';

import { useSnackbar } from '@/shared/hooks';

import { rolesApi, usersApi } from './users.api';
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

export function useUpdateUserRoles(id: number) {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['users', 'roles'],
    mutationFn: (request: UpdateUserRolesRequest) => usersApi.updateRoles(id, request),
    onSuccess: (access) => {
      qc.setQueryData(userKeys.access(scope, id), access);
      snackbar.success(`Roles of ${access.userName} saved.`);
    },
  });
}

export interface CreateRolesInput {
  request: Omit<SaveRoleRequest, 'supplierCodeId'>;
  supplierCodes: readonly SupplierCodeResponse[];
  pageActionIds: readonly number[];
}

export interface CreateRoleOutcome {
  supplierCode: SupplierCodeResponse;
  role: RoleResponse | null;
  error: unknown;
}

export function useCreateRoles() {
  const qc = useQueryClient();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['roles', 'create'],
    meta: { silent: true },
    mutationFn: async ({ request, supplierCodes, pageActionIds }: CreateRolesInput) => {
      const outcomes: CreateRoleOutcome[] = [];
      for (const supplierCode of supplierCodes) {
        let role: RoleResponse | null = null;
        try {
          role = await rolesApi.create({ ...request, supplierCodeId: supplierCode.id });
          if (pageActionIds.length > 0) {
            await rolesApi.updateRights(role.id, { pageActionIds: [...pageActionIds] });
          }
          outcomes.push({ supplierCode, role, error: null });
        } catch (error) {
          outcomes.push({ supplierCode, role, error });
        }
      }
      return outcomes;
    },
    onSettled: async () => {
      await qc.invalidateQueries({ queryKey: userKeys.roles(scope) });
    },
  });
}

export interface SaveRoleInput {
  request: SaveRoleRequest | null;
  pageActionIds: readonly number[] | null;
}

export function useSaveRole(id: number) {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['roles', 'update'],
    meta: { silent: true },
    mutationFn: async ({ request, pageActionIds }: SaveRoleInput) => {
      if (request) await rolesApi.update(id, request);
      if (pageActionIds) await rolesApi.updateRights(id, { pageActionIds: [...pageActionIds] });
    },
    onSettled: async () => {
      await qc.invalidateQueries({ queryKey: userKeys.roles(scope) });
    },
    onSuccess: () => {
      snackbar.success('Role saved.');
    },
  });
}

export function useDeleteRole() {
  const qc = useQueryClient();
  const snackbar = useSnackbar();
  const scope = useCompanyScope();
  return useMutation({
    mutationKey: ['roles', 'delete'],
    mutationFn: (role: Pick<RoleResponse, 'id' | 'displayName'>) => rolesApi.remove(role.id),
    onSuccess: async (_data, role) => {
      qc.removeQueries({ queryKey: userKeys.role(scope, role.id) });
      qc.removeQueries({ queryKey: userKeys.roleRights(scope, role.id) });
      await qc.invalidateQueries({ queryKey: userKeys.roles(scope) });
      snackbar.success(`Role ${role.displayName} deleted.`);
    },
  });
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
