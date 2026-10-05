import type { ModuleFlagsResponse, ScreenConfigResponse, TenantContextResponse } from '../api/contracts';
import { http } from '../api/http';

/**
 * The three start-up calls that configure the application for one installation (§8). They live in
 * `core` because every layer depends on their answers and no business module owns them.
 */
/** `exactOptionalPropertyTypes` forbids passing `signal: undefined`, so the key is omitted instead. */
function config(signal: AbortSignal | undefined): { signal?: AbortSignal } {
  return signal ? { signal } : {};
}

export const platformApi = {
  async modules(signal?: AbortSignal): Promise<ModuleFlagsResponse> {
    const { data } = await http.get<ModuleFlagsResponse>('/app/modules', config(signal));
    return data;
  },

  async tenant(signal?: AbortSignal): Promise<TenantContextResponse> {
    const { data } = await http.get<TenantContextResponse>('/app/tenant', config(signal));
    return data;
  },

  async screenConfig(screenKey: string, signal?: AbortSignal): Promise<ScreenConfigResponse> {
    const { data } = await http.get<ScreenConfigResponse>(`/app/screen-config/${screenKey}`, config(signal));
    return data;
  },
};
