import type { PermissionKey } from '@/core/auth';
import type { ModuleKey } from '@/core/modules';
import { PATHS } from '@/core/router';

export interface MenuItem {
  label: string;
  to: string;
  /** The module this item belongs to. A module the installation does not have is not rendered (§8.2). */
  module: ModuleKey;
  /** Any one of these rights shows the item. Omit for items every user may open. */
  permission?: readonly PermissionKey[];
}

export interface MenuGroup {
  title: string;
  items: readonly MenuItem[];
}

/**
 * The one definition of the sidebar.
 *
 * Two invariants hold here, and the Sidebar enforces both:
 *  - rights must match the `permission` of the route in the module's routes.tsx, so what the menu shows
 *    and what the route allows always agree;
 *  - `module` must match the route's `module`, so a flagged-off module disappears from the menu at the
 *    same moment its address stops resolving.
 */
export const MENU: readonly MenuGroup[] = [
  {
    title: 'General',
    items: [{ label: 'Dashboard', to: PATHS.dashboard, module: 'dashboard' }],
  },
  {
    title: 'Masters',
    items: [
      { label: 'Company', to: PATHS.company.list, module: 'company', permission: ['company.view', 'company.manage'] },
      { label: 'Plant', to: PATHS.plant.list, module: 'plant', permission: ['plant.view', 'plant.manage'] },
      { label: 'Brands', to: PATHS.brands.list, module: 'brands', permission: ['brands.view', 'brands.manage'] },
      { label: 'Batches', to: PATHS.batches.list, module: 'batches', permission: ['batches.view', 'batches.manage'] },
      { label: 'Production plans', to: PATHS.plans.list, module: 'plans', permission: ['plans.view', 'plans.manage'] },
      { label: 'Palette', to: PATHS.palette.list, module: 'palette', permission: ['palette.view', 'palette.manage'] },
    ],
  },
  {
    title: 'Production',
    items: [
      {
        label: 'Code pool',
        to: PATHS.codePool.list,
        module: 'code-pool',
        permission: ['code-pool.view', 'code-pool.download'],
      },
      {
        label: 'Case data',
        to: PATHS.caseData.search,
        module: 'case-data',
        permission: ['case-data.view', 'case-data.export'],
      },
      {
        label: 'Dispatch',
        to: PATHS.dispatch.list,
        module: 'dispatch',
        permission: ['dispatch.view', 'dispatch.manage'],
      },
    ],
  },
  {
    title: 'Integration',
    items: [
      {
        label: 'Portal sync',
        to: PATHS.portalSync.list,
        module: 'portal-sync',
        permission: ['portal-sync.view', 'portal-sync.run'],
      },
      { label: 'Outbox', to: PATHS.outbox.list, module: 'outbox', permission: ['outbox.view', 'outbox.retry'] },
    ],
  },
  {
    title: 'Reports',
    items: [
      {
        label: 'User activity',
        to: PATHS.reports.activity,
        module: 'reports',
        permission: ['reports.view', 'reports.export'],
      },
    ],
  },
  {
    title: 'Administration',
    items: [
      { label: 'Users', to: PATHS.users.list, module: 'users', permission: ['users.view', 'users.manage'] },
      {
        label: 'Security settings',
        to: PATHS.settings.security,
        module: 'settings',
        permission: ['settings.view', 'settings.manage'],
      },
      {
        label: 'Password policies',
        to: PATHS.settings.passwordPolicies,
        module: 'settings',
        permission: ['settings.view', 'settings.manage'],
      },
      { label: 'Licence', to: PATHS.license.status, module: 'license', permission: ['license.view'] },
    ],
  },
  {
    title: 'My account',
    items: [{ label: 'My sessions', to: PATHS.settings.sessions, module: 'settings' }],
  },
];
