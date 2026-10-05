/**
 * The seventeen business modules of the merged platform (Frontend Architecture Specification §3.2).
 *
 * This list is infrastructure, not business logic: it says which modules the platform *can* have and
 * which of them an installation may switch off. It deliberately holds no screens, no API calls and no
 * excise names — `core` never imports `features`.
 */
export const MODULE_KEYS = [
  'auth',
  'dashboard',
  'company',
  'plant',
  'users',
  'brands',
  'batches',
  'plans',
  'code-pool',
  'palette',
  'case-data',
  'dispatch',
  'portal-sync',
  'outbox',
  'reports',
  'settings',
  'license',
] as const;

export type ModuleKey = (typeof MODULE_KEYS)[number];

/**
 * `always` — present in every installation; the flag is not consulted.
 * `flag`   — present only when the backend enables it for this installation (§8.2 feature flags).
 */
export type ModuleAvailability = 'always' | 'flag';

export interface ModuleDefinition {
  readonly key: ModuleKey;
  /** Shown in the sidebar and in the module allocation view. */
  readonly title: string;
  readonly availability: ModuleAvailability;
  /** One line describing what the module owns, per §3.2. */
  readonly responsibility: string;
}

export function isModuleKey(value: string): value is ModuleKey {
  return (MODULE_KEYS as readonly string[]).includes(value);
}
