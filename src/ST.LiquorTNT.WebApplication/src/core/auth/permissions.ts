export const API_PERMISSION_KEYS = [
  'user.view',
  'user.add',
  'user.edit',
  'user.status',
  'user.unlock',
  'user.access',
  'user.manageadmin',
  'role.view',
  'role.add',
  'role.edit',
  'role.delete',
  'securityconfig.view',
  'securityconfig.edit',
  'passwordpolicy.view',
  'passwordpolicy.edit',
  'company.view',
  'suppliercode.view',
  'suppliercode.add',
  'suppliercode.edit',
  'liquorcategory.view',
  'liquorcategory.add',
  'liquorcategory.edit',
] as const;

export const EVERYONE_PERMISSIONS = ['dashboard.view', 'sessions.manage'] as const;

export const PERMISSION_KEYS = [...EVERYONE_PERMISSIONS, ...API_PERMISSION_KEYS] as const;

export type PermissionKey = (typeof PERMISSION_KEYS)[number];

export function isPermissionKey(value: string): value is PermissionKey {
  return (PERMISSION_KEYS as readonly string[]).includes(value);
}

export function toPermissionSet(values: readonly string[] | undefined): ReadonlySet<PermissionKey> {
  const set = new Set<PermissionKey>();
  for (const v of values ?? []) if (isPermissionKey(v)) set.add(v);
  return set;
}

export function effectivePermissions(granted: readonly string[]): ReadonlySet<PermissionKey> {
  return toPermissionSet([...EVERYONE_PERMISSIONS, ...granted]);
}

export function hasPermission(rights: ReadonlySet<PermissionKey>, key: PermissionKey): boolean {
  return rights.has(key);
}

export function hasAnyPermission(rights: ReadonlySet<PermissionKey>, keys: readonly PermissionKey[]): boolean {
  return keys.some((k) => rights.has(k));
}
