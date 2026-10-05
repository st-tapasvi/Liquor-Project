import { useTenantStore } from './tenant.store';

export interface Tenant {
  companyId: number | null;
  companyName: string | null;
  exciseCode: string | null;
  plantId: number | null;
  plantName: string | null;
}

/** The current company/plant/excise for display. Never branch on `exciseCode` (§8.1). */
export function useTenant(): Tenant {
  return useTenantStore((s) => ({
    companyId: s.companyId,
    companyName: s.companyName,
    exciseCode: s.exciseCode,
    plantId: s.plantId,
    plantName: s.plantName,
  }));
}

/**
 * The company every cache key must be scoped by. Returns 0 when no company is selected, so a key is
 * still well-formed and can never silently collide with a real company's cache.
 */
export function useCompanyScope(): number {
  return useTenantStore((s) => s.companyId ?? 0);
}
