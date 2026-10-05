import { type ModuleDefinition, type ModuleKey, MODULE_KEYS } from './module.types';

/**
 * The module table of specification §3.2, verbatim. Adding an excise never changes this file; adding a
 * *module* does. `availability: 'flag'` marks the six modules that exist only for some installations.
 */
export const MODULES: Readonly<Record<ModuleKey, ModuleDefinition>> = {
  auth: {
    key: 'auth',
    title: 'Authentication',
    availability: 'always',
    responsibility: 'Login (password / PIN), password recovery, company selection',
  },
  dashboard: {
    key: 'dashboard',
    title: 'Dashboard',
    availability: 'always',
    responsibility: 'Landing screen; tiles filtered by rights and flags',
  },
  company: {
    key: 'company',
    title: 'Company',
    availability: 'always',
    responsibility: 'Company master',
  },
  plant: {
    key: 'plant',
    title: 'Plant',
    availability: 'flag',
    responsibility: 'Plant master',
  },
  users: {
    key: 'users',
    title: 'Users',
    availability: 'always',
    responsibility: 'User master and rights tree',
  },
  brands: {
    key: 'brands',
    title: 'Brands',
    availability: 'always',
    responsibility: 'Brand master; brand synchronisation from excise portal',
  },
  batches: {
    key: 'batches',
    title: 'Batches',
    availability: 'always',
    responsibility: 'Batch master',
  },
  plans: {
    key: 'plans',
    title: 'Production plans',
    availability: 'flag',
    responsibility: 'Production plan master (portal-driven)',
  },
  'code-pool': {
    key: 'code-pool',
    title: 'Code pool',
    availability: 'flag',
    responsibility: 'Case / bottle code download and allocation',
  },
  palette: {
    key: 'palette',
    title: 'Palette',
    availability: 'flag',
    responsibility: 'Palette master',
  },
  'case-data': {
    key: 'case-data',
    title: 'Case data',
    availability: 'always',
    responsibility: 'Case report, case search, aggregation delete, exports',
  },
  dispatch: {
    key: 'dispatch',
    title: 'Dispatch',
    availability: 'flag',
    responsibility: 'Dispatch import, verification, portal submission, shift, undo',
  },
  'portal-sync': {
    key: 'portal-sync',
    title: 'Portal sync',
    availability: 'always',
    responsibility: 'Master import from excise database, portal API or Excel',
  },
  outbox: {
    key: 'outbox',
    title: 'Outbox',
    availability: 'flag',
    responsibility: 'Outbound submission queue and retry monitoring',
  },
  reports: {
    key: 'reports',
    title: 'Reports',
    availability: 'always',
    responsibility: 'User activity log, dispatch report, hologram wastage report',
  },
  settings: {
    key: 'settings',
    title: 'Settings',
    availability: 'always',
    responsibility: 'Application settings',
  },
  license: {
    key: 'license',
    title: 'Licence',
    availability: 'always',
    responsibility: 'Licence status, about, module allocation view',
  },
};

/** The six modules an installation may switch off. */
export const FLAGGED_MODULE_KEYS: readonly ModuleKey[] = MODULE_KEYS.filter(
  (key) => MODULES[key].availability === 'flag',
);
