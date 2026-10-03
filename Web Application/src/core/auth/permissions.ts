/**
 * Closed set of rights the UI understands. They map 1:1 to what the API grants in `permissions`.
 * Adding a right = adding it here AND in the API. A string that is not in this list is ignored.
 *
 * Convention (specification §7): `module.action`, where `module` is the module key from
 * `core/modules`. Every module has `view` and `manage`; modules with a distinct privileged action
 * name it explicitly (`dispatch.send`, `code-pool.download`, `case-data.delete`).
 *
 * A right is a usability signal only. It decides what is shown or enabled; the API enforces every
 * call regardless (§9), so hiding a control is never the security boundary.
 */
export const PERMISSION_KEYS = [
  // --- always-present modules ---
  'dashboard.view',
  'company.view',
  'company.manage',
  'users.view',
  'users.manage',
  'brands.view',
  'brands.manage',
  'brands.sync',
  'batches.view',
  'batches.manage',
  'case-data.view',
  'case-data.export',
  'case-data.delete',
  'portal-sync.view',
  'portal-sync.run',
  'reports.view',
  'reports.export',
  'settings.view',
  'settings.manage',
  'license.view',
  'sessions.manage',

  // --- flag-controlled modules ---
  'plant.view',
  'plant.manage',
  'plans.view',
  'plans.manage',
  'code-pool.view',
  'code-pool.download',
  'palette.view',
  'palette.manage',
  'dispatch.view',
  'dispatch.manage',
  'dispatch.send',
  'outbox.view',
  'outbox.retry',

  // --- diagnostics (§10.2: detailed logging is permission-gated and audited) ---
  'diagnostics.detailed-logging',
] as const;

export type PermissionKey = (typeof PERMISSION_KEYS)[number];

export function isPermissionKey(value: string): value is PermissionKey {
  return (PERMISSION_KEYS as readonly string[]).includes(value);
}

/** Keeps only the rights this build knows about; unknown strings from a newer API are dropped. */
export function toPermissionSet(values: readonly string[] | undefined): ReadonlySet<PermissionKey> {
  const set = new Set<PermissionKey>();
  for (const v of values ?? []) if (isPermissionKey(v)) set.add(v);
  return set;
}

export const EVERYONE_PERMISSIONS: readonly PermissionKey[] = ['dashboard.view', 'sessions.manage'];
export const INTERIM_ADMIN_ROLE_ID = 1;

export function interimRights(roleId: number | null): {
  isAdministrator: boolean;
  permissions: readonly PermissionKey[];
} {
  const isAdministrator = roleId === INTERIM_ADMIN_ROLE_ID;
  return { isAdministrator, permissions: isAdministrator ? PERMISSION_KEYS : EVERYONE_PERMISSIONS };
}

export function hasPermission(rights: ReadonlySet<PermissionKey>, key: PermissionKey): boolean {
  return rights.has(key);
}

export function hasAnyPermission(rights: ReadonlySet<PermissionKey>, keys: readonly PermissionKey[]): boolean {
  return keys.some((k) => rights.has(k));
}
