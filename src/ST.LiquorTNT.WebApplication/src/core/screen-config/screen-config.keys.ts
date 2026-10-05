import type { ModuleKey } from '../modules';

/**
 * Screen keys the frontend may ask for. A closed list, so a typo is a compile error rather than a
 * silent 404 at runtime. `module.screen`, matching the permission-key convention of §7.
 */
export const SCREEN_KEYS = [
  'brands.form',
  'batches.form',
  'plans.form',
  'palette.form',
  'case-data.search',
  'dispatch.form',
] as const;

export type ScreenKey = (typeof SCREEN_KEYS)[number];

/** Cache keys for screen configuration, scoped by excise: two excises never share a cached screen. */
export const screenConfigKeys = {
  all: (exciseCode: string) => ['screen-config', exciseCode] as const,
  byScreen: (exciseCode: string, screenKey: ScreenKey) => ['screen-config', exciseCode, screenKey] as const,
};

/** The module a screen belongs to, for flag checks and logging. */
export function moduleOfScreen(screenKey: ScreenKey): ModuleKey {
  return screenKey.split('.')[0] as ModuleKey;
}
