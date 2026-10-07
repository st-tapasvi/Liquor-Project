import type { PermissionKey } from '@/core/auth';
import type { ModuleKey } from '@/core/modules';
import { PATHS } from '@/core/router';

export interface MenuLink {
  label: string;
  to: string;
  permission?: readonly PermissionKey[];
}

export interface MenuItem extends MenuLink {
  /** The module this item belongs to: its icon, and whether the installation has it (§8.2). */
  module: ModuleKey;
  /** Sub-pages shown under the item. The item itself is shown when at least one child is visible. */
  children?: readonly MenuLink[];
}

/**
 * The one definition of the sidebar (Penpot "2.1 App shell dashboard": one flat list, one entry per module).
 *
 * Two invariants hold here, and the Sidebar enforces both:
 *  - rights must match the `permission` of the route in the module's routes.tsx, so what the menu shows
 *    and what the route allows always agree;
 *  - `module` must match the route's `module`, so a flagged-off module disappears from the menu at the
 *    same moment its address stops resolving.
 */
export const MENU: readonly MenuItem[] = [
  { label: 'Dashboard', to: PATHS.dashboard, module: 'dashboard' },
  { label: 'Company', to: PATHS.company.list, module: 'company', permission: ['company.view', 'company.manage'] },
  { label: 'Plant', to: PATHS.plant.list, module: 'plant', permission: ['plant.view', 'plant.manage'] },
  { label: 'Users', to: PATHS.users.list, module: 'users', permission: ['users.view', 'users.manage'] },
  { label: 'Brands', to: PATHS.brands.list, module: 'brands', permission: ['brands.view', 'brands.manage'] },
  { label: 'Batches', to: PATHS.batches.list, module: 'batches', permission: ['batches.view', 'batches.manage'] },
  { label: 'Plans', to: PATHS.plans.list, module: 'plans', permission: ['plans.view', 'plans.manage'] },
  {
    label: 'Code Pool',
    to: PATHS.codePool.list,
    module: 'code-pool',
    permission: ['code-pool.view', 'code-pool.download'],
  },
  { label: 'Palette', to: PATHS.palette.list, module: 'palette', permission: ['palette.view', 'palette.manage'] },
  {
    label: 'Case Data',
    to: PATHS.caseData.search,
    module: 'case-data',
    permission: ['case-data.view', 'case-data.export'],
  },
  { label: 'Dispatch', to: PATHS.dispatch.list, module: 'dispatch', permission: ['dispatch.view', 'dispatch.manage'] },
  {
    label: 'Portal Sync',
    to: PATHS.portalSync.list,
    module: 'portal-sync',
    permission: ['portal-sync.view', 'portal-sync.run'],
  },
  { label: 'Outbox', to: PATHS.outbox.list, module: 'outbox', permission: ['outbox.view', 'outbox.retry'] },
  {
    label: 'Reports',
    to: PATHS.reports.activity,
    module: 'reports',
    permission: ['reports.view', 'reports.export'],
  },
  {
    label: 'Settings',
    to: PATHS.settings.security,
    module: 'settings',
    children: [
      { label: 'Security settings', to: PATHS.settings.security, permission: ['settings.view', 'settings.manage'] },
      {
        label: 'Password policies',
        to: PATHS.settings.passwordPolicies,
        permission: ['settings.view', 'settings.manage'],
      },
      { label: 'My sessions', to: PATHS.settings.sessions },
    ],
  },
  { label: 'About / Licence', to: PATHS.license.status, module: 'license', permission: ['license.view'] },
];
