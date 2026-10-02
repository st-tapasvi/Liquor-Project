import { isModuleEnabled, useModuleFlagsStore } from './module-flags.store';
import { MODULE_KEYS } from './module.types';
import { FLAGGED_MODULE_KEYS, MODULES } from './modules.registry';

describe('module flags', () => {
  it('registers all seventeen modules of specification §3.2', () => {
    expect(MODULE_KEYS).toHaveLength(17);
    expect(Object.keys(MODULES)).toHaveLength(17);
    // Every key in the list has a definition, and every definition agrees with its key.
    for (const key of MODULE_KEYS) expect(MODULES[key].key).toBe(key);
  });

  it('marks exactly the six optional modules as flag-controlled', () => {
    expect([...FLAGGED_MODULE_KEYS].sort()).toEqual(
      ['code-pool', 'dispatch', 'outbox', 'palette', 'plans', 'plant'].sort(),
    );
  });

  it('treats an always-present module as enabled even before the flags arrive', () => {
    const none = new Set<never>();
    expect(isModuleEnabled('brands', none)).toBe(true);
    expect(isModuleEnabled('users', none)).toBe(true);
  });

  it('treats a flagged module as absent until the backend enables it', () => {
    const none = new Set<never>();
    // Defaulting to off means a failed flag request can only hide a module, never expose one.
    expect(isModuleEnabled('dispatch', none)).toBe(false);
    expect(isModuleEnabled('dispatch', new Set(['dispatch'] as const))).toBe(true);
  });

  it('ignores module keys a newer API sends that this build does not know', () => {
    useModuleFlagsStore.getState().setFlags(['dispatch', 'time-travel']);
    const { enabled, loaded } = useModuleFlagsStore.getState();

    expect(loaded).toBe(true);
    expect(enabled.has('dispatch')).toBe(true);
    expect([...enabled]).toEqual(['dispatch']);
  });
});
