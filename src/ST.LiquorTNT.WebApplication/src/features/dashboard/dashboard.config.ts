import type { PermissionKey } from '@/core/auth';
import type { ModuleKey } from '@/core/modules';
import { PATHS } from '@/core/router';

export interface ModuleTileDef {
  module: ModuleKey;
  label: string;
  to: string;
  /** Any one of these rights shows the tile; the same rights as the module's sidebar item and route. */
  permission: readonly PermissionKey[];
}

/** The tiles of the design, in its order. A module that is not enabled is shown greyed out, not hidden. */
export const MODULE_TILES: readonly ModuleTileDef[] = [
  { module: 'company', label: 'Company', to: PATHS.company.list, permission: ['company.view', 'company.manage'] },
  { module: 'users', label: 'Users', to: PATHS.users.list, permission: ['users.view', 'users.manage'] },
  { module: 'brands', label: 'Brands', to: PATHS.brands.list, permission: ['brands.view', 'brands.manage'] },
  { module: 'batches', label: 'Batches', to: PATHS.batches.list, permission: ['batches.view', 'batches.manage'] },
  {
    module: 'case-data',
    label: 'Case Data',
    to: PATHS.caseData.search,
    permission: ['case-data.view', 'case-data.export'],
  },
  {
    module: 'dispatch',
    label: 'Dispatch',
    to: PATHS.dispatch.list,
    permission: ['dispatch.view', 'dispatch.manage', 'dispatch.send'],
  },
  {
    module: 'portal-sync',
    label: 'Portal Sync',
    to: PATHS.portalSync.list,
    permission: ['portal-sync.view', 'portal-sync.run'],
  },
  { module: 'reports', label: 'Reports', to: PATHS.reports.activity, permission: ['reports.view', 'reports.export'] },
  {
    module: 'settings',
    label: 'Settings',
    to: PATHS.settings.security,
    permission: ['settings.view', 'settings.manage'],
  },
  { module: 'license', label: 'About / Licence', to: PATHS.license.status, permission: ['license.view'] },
  { module: 'plans', label: 'Plans', to: PATHS.plans.list, permission: ['plans.view', 'plans.manage'] },
  { module: 'palette', label: 'Palette', to: PATHS.palette.list, permission: ['palette.view', 'palette.manage'] },
];

/** "view · manage": the user's own rights in a module, read from the `module.action` keys. */
export function rightsLabel(module: ModuleKey, rights: ReadonlySet<PermissionKey>): string {
  const prefix = `${module}.`;
  return [...rights]
    .filter((r) => r.startsWith(prefix))
    .map((r) => r.slice(prefix.length))
    .join(' · ');
}

export interface Kpi {
  label: string;
  /** null until the figure has a data source; the card then shows "—". */
  value: string | null;
  caption: string;
}

/**
 * Today's figures. The API has no endpoint for them yet (production, dispatch and outbox modules are still
 * placeholders), so each shows "—" with the reason instead of invented numbers. Wire `value` to a query
 * when the endpoint exists.
 */
export const KPIS: readonly Kpi[] = [
  { label: 'Cases today', value: null, caption: 'available with Case Data' },
  { label: 'Bottles today', value: null, caption: 'available with Case Data' },
  { label: 'Pending push', value: null, caption: 'available with Outbox' },
  { label: 'Active batches', value: null, caption: 'available with Batches' },
];
