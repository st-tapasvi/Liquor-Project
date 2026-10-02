import Box from '@mui/material/Box';
import { DataGrid, type GridColDef, type GridRowId, type GridValidRowModel } from '@mui/x-data-grid';

import type { PagedResult } from '@/core/api';

import { ErrorState } from '@/shared/components/feedback';

import type { useServerGrid } from './useServerGrid';

export type AppGridColumn<TRow extends GridValidRowModel> = GridColDef<TRow>;

interface AppDataGridProps<TRow extends GridValidRowModel> {
  columns: readonly AppGridColumn<TRow>[];
  /** The page currently held by the query (undefined while loading the first time). */
  data: PagedResult<TRow> | undefined;
  isLoading: boolean;
  error?: unknown;
  onRetry?: () => void;
  grid: ReturnType<typeof useServerGrid>;
  getRowId: (row: TRow) => GridRowId;
  /** Accessible name for the table. */
  ariaLabel: string;
  minHeight?: number;
}

/**
 * The project's grid: MUI X DataGrid (Community, MIT) in server-side pagination mode. Every list screen
 * uses this wrapper, never `@mui/x-data-grid` directly, so paging, density, empty/error states and
 * accessibility are decided once.
 */
export function AppDataGrid<TRow extends GridValidRowModel>({
  columns,
  data,
  isLoading,
  error,
  onRetry,
  grid,
  getRowId,
  ariaLabel,
  minHeight = 420,
}: AppDataGridProps<TRow>) {
  if (error) {
    return <ErrorState error={error} {...(onRetry ? { onRetry } : {})} />;
  }

  return (
    <Box sx={{ minHeight, width: '100%' }}>
      <DataGrid<TRow>
        aria-label={ariaLabel}
        columns={columns as GridColDef<TRow>[]}
        rows={data?.items ?? []}
        rowCount={data?.totalCount ?? 0}
        getRowId={getRowId}
        loading={isLoading}
        density="compact"
        disableRowSelectionOnClick
        disableColumnMenu
        paginationMode="server"
        sortingMode="server"
        filterMode="server"
        paginationModel={{ page: grid.state.page - 1, pageSize: grid.state.pageSize }}
        onPaginationModelChange={(model) => {
          if (model.pageSize !== grid.state.pageSize) grid.setPageSize(model.pageSize);
          else grid.setPage(model.page + 1);
        }}
        pageSizeOptions={[...grid.pageSizeOptions]}
        sx={{ border: 0, '& .MuiDataGrid-cell:focus, & .MuiDataGrid-columnHeader:focus': { outline: 'none' } }}
      />
    </Box>
  );
}
