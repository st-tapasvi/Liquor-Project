import type { RoleResponse } from '@/core/api';

export interface RoleOption {
  id: number;
  label: string;
  locked?: boolean;
}

export function roleLabel(role: Pick<RoleResponse, 'displayName' | 'roleName'>): string {
  return role.displayName || role.roleName;
}

export function assignableRoles(roles: readonly RoleResponse[], companyId?: number | null): RoleResponse[] {
  return roles.filter(
    (r) => r.isActive && !r.isSystem && !r.isTemplate && (companyId === undefined || r.companyId === companyId),
  );
}
