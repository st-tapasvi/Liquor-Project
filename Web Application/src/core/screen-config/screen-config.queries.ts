import { useQuery } from '@tanstack/react-query';

import type { FieldConfig, ScreenConfigResponse } from '../api/contracts';
import { platformApi } from '../modules';
import { useTenantStore } from '../tenant';

import { type ScreenKey, screenConfigKeys } from './screen-config.keys';

/**
 * The field definitions for one screen, for the excise of the selected company (§8.2).
 *
 * Configuration changes far less often than data, so it is cached for the session and only re-fetched
 * when the excise changes. Every consumer gets the same object, so a screen never renders one set of
 * fields while another renders a different set.
 */
export function useScreenConfig(screenKey: ScreenKey) {
  const exciseCode = useTenantStore((s) => s.exciseCode);

  return useQuery({
    queryKey: screenConfigKeys.byScreen(exciseCode ?? '', screenKey),
    queryFn: ({ signal }) => platformApi.screenConfig(screenKey, signal),
    // No company selected means no excise, so there is nothing meaningful to ask for yet.
    enabled: exciseCode !== null,
    staleTime: 30 * 60_000,
    gcTime: 60 * 60_000,
  });
}

/** The visible fields of a screen configuration, in the order the backend asked for. */
export function visibleFields(config: ScreenConfigResponse | undefined): FieldConfig[] {
  if (!config) return [];
  return config.fields.filter((f) => f.visible).sort((a, b) => a.sequence - b.sequence);
}

/** Looks a field up by name. Returns undefined when this excise does not have the field at all. */
export function fieldByName(config: ScreenConfigResponse | undefined, name: string): FieldConfig | undefined {
  return config?.fields.find((f) => f.name === name);
}
