import type { AxiosInstance } from 'axios';

import { currentTenant } from '../../tenant/tenant.store';

export const TENANT_HEADERS = {
  company: 'X-Company-Id',
  plant: 'X-Plant-Id',
} as const;

/**
 * Attaches the selected company and plant to every request, in one place (§9).
 *
 * Doing this centrally is what makes tenant isolation reviewable: no screen can forget it, and no screen
 * can override it. The API still decides what the user may see — these headers are a scope hint the API
 * validates against the session, never an authorisation claim.
 */
export function installTenantInterceptor(instance: AxiosInstance): void {
  instance.interceptors.request.use((config) => {
    const { companyId, plantId } = currentTenant();
    if (companyId !== null) config.headers.set(TENANT_HEADERS.company, String(companyId));
    if (plantId !== null) config.headers.set(TENANT_HEADERS.plant, String(plantId));
    return config;
  });
}
