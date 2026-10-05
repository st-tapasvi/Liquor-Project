import type { UserListParams } from '@/core/api';

/**
 * Query-key factory for the users module (specification §7: `name.keys.ts`).
 *
 * Every key is scoped by company. Together with the cache wipe on company switch (core/tenant) this
 * gives two independent guarantees that one company's rows can never be rendered under another — the
 * scoping holds even if a future change removes the wipe (§9, tenant isolation).
 */
export const userKeys = {
  all: (companyId: number) => ['users', companyId] as const,
  lists: (companyId: number) => [...userKeys.all(companyId), 'list'] as const,
  list: (companyId: number, params: UserListParams) => [...userKeys.lists(companyId), params] as const,
  detail: (companyId: number, id: number) => [...userKeys.all(companyId), 'detail', id] as const,
};
