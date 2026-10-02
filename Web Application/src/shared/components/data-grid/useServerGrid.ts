import { useCallback, useMemo } from 'react';
import { useSearchParams } from 'react-router';

import { DEFAULT_PAGE_SIZE, MAX_PAGE_SIZE, PAGE_SIZE_OPTIONS } from '@/shared/constants';

export interface ServerGridState {
  /** 1-based page as the API expects. */
  page: number;
  pageSize: number;
  search: string;
}

/**
 * Keeps grid paging and the search text in the URL (`?page=2&pageSize=50&search=ravi`) so a screen is
 * bookmarkable, survives refresh and can be shared. Values are clamped to what the API accepts.
 */
export function useServerGrid(defaults?: Partial<ServerGridState>) {
  const [params, setParams] = useSearchParams();

  const state = useMemo<ServerGridState>(() => {
    const page = clampInt(params.get('page'), 1, Number.MAX_SAFE_INTEGER, defaults?.page ?? 1);
    const pageSize = clampInt(params.get('pageSize'), 1, MAX_PAGE_SIZE, defaults?.pageSize ?? DEFAULT_PAGE_SIZE);
    const search = (params.get('search') ?? defaults?.search ?? '').slice(0, 100);
    return { page, pageSize, search };
  }, [params, defaults?.page, defaults?.pageSize, defaults?.search]);

  const update = useCallback(
    (patch: Partial<ServerGridState>) => {
      setParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          const merged = { ...state, ...patch };
          // A new search always starts at page 1.
          if (patch.search !== undefined && patch.search !== state.search) merged.page = 1;
          setOrDelete(next, 'page', merged.page === 1 ? '' : String(merged.page));
          setOrDelete(next, 'pageSize', merged.pageSize === DEFAULT_PAGE_SIZE ? '' : String(merged.pageSize));
          setOrDelete(next, 'search', merged.search);
          return next;
        },
        { replace: true },
      );
    },
    [setParams, state],
  );

  return {
    state,
    setPage: (page: number) => update({ page }),
    setPageSize: (pageSize: number) => update({ pageSize }),
    setSearch: (search: string) => update({ search }),
    pageSizeOptions: PAGE_SIZE_OPTIONS,
  };
}

function clampInt(raw: string | null, min: number, max: number, fallback: number): number {
  if (raw === null) return fallback;
  const n = Number.parseInt(raw, 10);
  if (!Number.isFinite(n)) return fallback;
  return Math.min(max, Math.max(min, n));
}

function setOrDelete(params: URLSearchParams, key: string, value: string) {
  if (value) params.set(key, value);
  else params.delete(key);
}
