import { type ReactNode, useEffect } from 'react';

import { useSessionStore } from '../auth/session.store';
import { logger } from '../logging/logger';
import { platformApi, useModuleFlagsStore } from '../modules';
import { useTenantStore } from '../tenant';

interface PlatformProviderProps {
  children: ReactNode;
  /** Shown while the installation's modules and tenant context are being fetched. */
  fallback: ReactNode;
}

/**
 * Loads the two things that configure the application for one installation (specification §8, §9):
 * which modules exist, and which companies and plants the signed-in user may work in.
 *
 * Both are fetched once per session, after authentication — they are per-installation and per-user, so
 * there is nothing to ask for while anonymous — and both are dropped when the session ends, so the next
 * user never inherits the previous user's companies or the previous installation's flags.
 *
 * Children are gated until the answers arrive. Rendering the shell first would flash a sidebar without
 * the optional modules and could send a request before the tenant headers exist.
 */
export function PlatformProvider({ children, fallback }: PlatformProviderProps) {
  const status = useSessionStore((s) => s.status);

  const flagsLoaded = useModuleFlagsStore((s) => s.loaded);
  const setFlags = useModuleFlagsStore((s) => s.setFlags);
  const resetFlags = useModuleFlagsStore((s) => s.reset);

  const tenantLoaded = useTenantStore((s) => s.loaded);
  const setTenantContext = useTenantStore((s) => s.setContext);
  const resetTenant = useTenantStore((s) => s.reset);

  // Why an effect: a one-time network call whose result decides what the shell may render.
  useEffect(() => {
    if (status !== 'authenticated' || (flagsLoaded && tenantLoaded)) return;
    const controller = new AbortController();

    void (async () => {
      const [modules, tenant] = await Promise.allSettled([
        platformApi.modules(controller.signal),
        platformApi.tenant(controller.signal),
      ]);

      if (controller.signal.aborted) return;

      if (modules.status === 'fulfilled') {
        setFlags(modules.value.enabled);
      } else {
        // Every optional module stays off. The core platform still works, and support sees why.
        logger.error('module flags could not be loaded; optional modules are hidden', { error: modules.reason });
        setFlags([]);
      }

      if (tenant.status === 'fulfilled') {
        setTenantContext({
          companies: tenant.value.companies,
          plants: tenant.value.plants,
          companyId: tenant.value.defaultCompanyId,
          plantId: tenant.value.defaultPlantId,
        });
      } else {
        logger.error('tenant context could not be loaded', { error: tenant.reason });
        setTenantContext({ companies: [], plants: [], companyId: null, plantId: null });
      }
    })();

    return () => {
      controller.abort();
    };
  }, [status, flagsLoaded, tenantLoaded, setFlags, setTenantContext]);

  // Why an effect: the flags and the tenant belong to the session that just ended.
  useEffect(() => {
    if (status === 'anonymous') {
      resetFlags();
      resetTenant();
    }
  }, [status, resetFlags, resetTenant]);

  if (status === 'authenticated' && !(flagsLoaded && tenantLoaded)) return fallback;
  return children;
}
