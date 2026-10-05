import { useModuleFlagsStore, isModuleEnabled } from './module-flags.store';
import type { ModuleKey } from './module.types';

/** True when this installation has the module. Use it to decide what to show, never what to allow. */
export function useModuleEnabled(key: ModuleKey): boolean {
  const enabled = useModuleFlagsStore((s) => s.enabled);
  return isModuleEnabled(key, enabled);
}

/** Filters any list of module-tagged items down to the modules this installation has. */
export function useEnabledModules<T extends { module: ModuleKey }>(items: readonly T[]): T[] {
  const enabled = useModuleFlagsStore((s) => s.enabled);
  return items.filter((item) => isModuleEnabled(item.module, enabled));
}

/** False only while the flag request is still in flight at start-up. */
export function useModuleFlagsLoaded(): boolean {
  return useModuleFlagsStore((s) => s.loaded);
}
