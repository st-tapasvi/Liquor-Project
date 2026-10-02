import { create } from 'zustand';

import type { ModuleKey } from './module.types';
import { isModuleKey } from './module.types';
import { MODULES } from './modules.registry';

/**
 * Which flagged modules this installation has. Supplied by the backend at start-up (§8.2); the frontend
 * never decides. Held in memory only — like the session, it is re-read on every page load.
 *
 * Until the answer arrives `loaded` is false and every flagged module is treated as OFF. Defaulting to
 * off rather than on means a slow or failed flag request can only ever hide a module, never expose one
 * an installation has not licensed.
 */
interface ModuleFlagsState {
  loaded: boolean;
  enabled: ReadonlySet<ModuleKey>;
  setFlags: (keys: readonly string[]) => void;
  reset: () => void;
}

export const useModuleFlagsStore = create<ModuleFlagsState>((set) => ({
  loaded: false,
  enabled: new Set<ModuleKey>(),
  setFlags: (keys) => {
    const next = new Set<ModuleKey>();
    // Unknown keys from a newer API are ignored, exactly as unknown permissions are.
    for (const key of keys) if (isModuleKey(key)) next.add(key);
    set({ loaded: true, enabled: next });
  },
  reset: () => {
    set({ loaded: false, enabled: new Set<ModuleKey>() });
  },
}));

/**
 * Whether a module exists for this installation. `always` modules never consult the flags, so the
 * menu and routes of the core platform work even if the flag request fails.
 */
export function isModuleEnabled(key: ModuleKey, enabled: ReadonlySet<ModuleKey>): boolean {
  return MODULES[key].availability === 'always' || enabled.has(key);
}
