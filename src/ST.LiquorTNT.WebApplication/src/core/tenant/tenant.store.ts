import { create } from 'zustand';

import type { CompanyOption, PlantOption } from '../api/contracts';
import { queryClient } from '../api/query-client';

/**
 * Which company, plant and excise the user is working in (specification §9, tenant isolation).
 *
 * Two rules make this a safety mechanism rather than a convenience:
 *  1. the identifiers are attached to every request centrally (tenant.interceptor), never per screen;
 *  2. switching company wipes the query cache, so one company's rows can never be shown under another.
 *
 * `exciseCode` is carried for display and for fetching screen configuration. It is NEVER branched on:
 * `if (exciseCode === 'UP')` is prohibited by §8.1 and by an ESLint rule.
 */
interface TenantState {
  companyId: number | null;
  companyName: string | null;
  exciseCode: string | null;
  plantId: number | null;
  plantName: string | null;

  companies: readonly CompanyOption[];
  plants: readonly PlantOption[];

  /** True once the tenant context has been fetched, whether or not a company was selected. */
  loaded: boolean;

  setContext: (context: {
    companies: readonly CompanyOption[];
    plants: readonly PlantOption[];
    companyId: number | null;
    plantId: number | null;
  }) => void;
  selectCompany: (companyId: number) => void;
  selectPlant: (plantId: number | null) => void;
  reset: () => void;
}

const EMPTY = {
  companyId: null,
  companyName: null,
  exciseCode: null,
  plantId: null,
  plantName: null,
  companies: [] as readonly CompanyOption[],
  plants: [] as readonly PlantOption[],
  loaded: false,
};

export const useTenantStore = create<TenantState>((set, get) => ({
  ...EMPTY,

  setContext: ({ companies, plants, companyId, plantId }) => {
    const company = companies.find((c) => c.companyId === companyId) ?? null;
    const plant = plants.find((p) => p.plantId === plantId) ?? null;
    set({
      companies,
      plants,
      loaded: true,
      companyId: company?.companyId ?? null,
      companyName: company?.companyName ?? null,
      exciseCode: company?.exciseCode ?? null,
      plantId: plant?.plantId ?? null,
      plantName: plant?.plantName ?? null,
    });
  },

  selectCompany: (companyId) => {
    if (get().companyId === companyId) return;
    const company = get().companies.find((c) => c.companyId === companyId);
    if (!company) return;

    // Everything cached belongs to the previous company. Drop it before anything can render.
    queryClient.clear();

    set({
      companyId: company.companyId,
      companyName: company.companyName,
      exciseCode: company.exciseCode,
      // A plant belongs to one company, so the previous selection cannot survive the switch.
      plantId: null,
      plantName: null,
    });
  },

  selectPlant: (plantId) => {
    if (plantId === null) {
      set({ plantId: null, plantName: null });
      return;
    }
    const plant = get().plants.find((p) => p.plantId === plantId && p.companyId === get().companyId);
    if (!plant) return;
    set({ plantId: plant.plantId, plantName: plant.plantName });
  },

  reset: () => {
    queryClient.clear();
    set({ ...EMPTY });
  },
}));

/** Read outside React (interceptors, cache-key factories). */
export function currentTenant(): { companyId: number | null; plantId: number | null } {
  const { companyId, plantId } = useTenantStore.getState();
  return { companyId, plantId };
}
